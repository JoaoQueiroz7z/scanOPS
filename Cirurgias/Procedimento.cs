using ScanOPS.Contagens;
using ScanOPS.Excecoes;
using ScanOPS.Instrumentos;
using ScanOPS.Pessoas;
using ScanOPS.Relatorios;

namespace ScanOPS.Cirurgias;

/// <summary>
/// Objeto central do domínio (raiz do agregado). Controla o ciclo de vida
/// Planejado -> EmAndamento -> Finalizado (ou Planejado -> Cancelado) e é o
/// único que altera o próprio Status e os seus itens de contagem.
///
/// Invariante protegida: procedimento Finalizado ou Cancelado não aceita
/// alterações de contagem, e nenhuma operação recusada deixa estado parcial.
/// Regras de negócio: ver comentários RN1..RN5 e docs/av2/especificacao.md.
/// </summary>
public class Procedimento
{
    private readonly List<ItemProcedimento> _itens = new();

    // Dados do procedimento: definidos na criação e fechados depois (nenhum setter público),
    // para que um procedimento encerrado (histórico) não possa ser alterado.
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Tipo { get; private set; } = "";
    public string Descricao { get; private set; } = "";
    public string Sala { get; private set; } = "";
    public DateTime? DataHoraInicio { get; private set; }
    public DateTime? DataHoraFim { get; private set; }   // fim da cirurgia ou momento do cancelamento
    public StatusProcedimento Status { get; private set; } = StatusProcedimento.Planejado;
    public string Observacoes { get; private set; } = "";
    public string? Justificativa { get; private set; }      // registrada ao finalizar com divergência (RN3)
    public string? MotivoCancelamento { get; private set; } // registrado ao cancelar

    public Paciente Paciente { get; private set; } = null!;
    public Usuario Cirurgiao { get; private set; } = null!;
    public Usuario Instrumentista { get; private set; } = null!;

    /// <summary>Procedimento sem dados de identificação (usado nos testes de regra e pelo EF Core).</summary>
    public Procedimento() { }

    /// <summary>Planeja um procedimento: nasce Planejado, com seus dados já definidos.</summary>
    public Procedimento(string tipo, string sala, string? descricao, Paciente paciente, Usuario cirurgiao, Usuario instrumentista)
    {
        Tipo = tipo;
        Sala = sala;
        Descricao = descricao ?? "";
        Paciente = paciente;
        Cirurgiao = cirurgiao;
        Instrumentista = instrumentista;
    }

    // Somente leitura para quem está fora: a lista só muda por AdicionarInstrumento.
    public IReadOnlyList<ItemProcedimento> Itens => _itens;

    // ===================== Transições de estado =====================

    // RN1: só é possível iniciar um procedimento Planejado.
    public void Iniciar()
    {
        if (Status != StatusProcedimento.Planejado)
            throw new RegraDeNegocioException("Só é possível iniciar um procedimento planejado.");

        Status = StatusProcedimento.EmAndamento;
        DataHoraInicio = DateTime.Now;
    }

    // RN2: só é possível finalizar um procedimento EmAndamento.
    // RN3: havendo divergência, a finalização exige justificativa.
    // Todas as validações acontecem ANTES de qualquer alteração: se algo for
    // recusado, o procedimento e os itens continuam exatamente como estavam.
    public void Finalizar(string? justificativa = null)
    {
        if (Status != StatusProcedimento.EmAndamento)
            throw new RegraDeNegocioException("Só é possível finalizar um procedimento em andamento.");

        bool possuiDivergencia = _itens.Any(i => i.PossuiDivergencia());
        if (possuiDivergencia && string.IsNullOrWhiteSpace(justificativa))
            throw new RegraDeNegocioException(
                "Há divergências na contagem. Informe uma justificativa para finalizar.");

        // A partir daqui nada mais pode falhar: aplica as alterações.
        foreach (var item in _itens)
            item.Conferir();

        if (possuiDivergencia)
        {
            Justificativa = justificativa!.Trim();
            Observacoes = $"{Observacoes}\n[Justificativa] {Justificativa}".Trim();
        }

        Status = StatusProcedimento.Finalizado;
        DataHoraFim = DateTime.Now;
    }

    // Cancelamento: só antes de começar (depois de iniciado, os instrumentos
    // já entraram em campo e precisam ser contados e finalizados).
    public void Cancelar(string motivo)
    {
        if (Status != StatusProcedimento.Planejado)
            throw new RegraDeNegocioException("Só é possível cancelar um procedimento planejado.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new RegraDeNegocioException("Informe o motivo do cancelamento.");

        MotivoCancelamento = motivo.Trim();
        Observacoes = $"{Observacoes}\n[Cancelamento] {MotivoCancelamento}".Trim();
        Status = StatusProcedimento.Cancelado;
        DataHoraFim = DateTime.Now; // para o histórico saber quando foi cancelado
    }

    // ===================== Itens e contagens =====================

    // Os itens nascem dentro do procedimento (composição) e só antes do início,
    // para que a contagem inicial cubra todos os instrumentos previstos.
    public ItemProcedimento AdicionarInstrumento(Instrumento instrumento, int quantidadePrevista = 1)
    {
        if (Status != StatusProcedimento.Planejado)
            throw new RegraDeNegocioException("Só é possível adicionar instrumentos a um procedimento planejado.");
        if (quantidadePrevista <= 0)
            throw new RegraDeNegocioException("A quantidade prevista deve ser maior que zero.");
        // RN6: instrumento inativo (fora de uso) não pode entrar num procedimento novo.
        if (!instrumento.Ativo)
            throw new RegraDeNegocioException($"O instrumento \"{instrumento.Nome}\" está inativo e não pode ser adicionado a um procedimento.");

        var item = new ItemProcedimento(instrumento, quantidadePrevista, sequencia: _itens.Count + 1);
        _itens.Add(item);
        return item;
    }

    // RN5: contagens só com procedimento EmAndamento e quantidade válida
    // (entrada > 0; saída >= 0, pois zero significa "nenhuma unidade voltou").
    public void RegistrarEntrada(Guid itemId, int quantidade)
    {
        GarantirContagemPermitida();
        if (quantidade <= 0)
            throw new RegraDeNegocioException("A quantidade de entrada deve ser maior que zero.");

        ObterItem(itemId).RegistrarEntrada(quantidade);
    }

    public void RegistrarSaida(Guid itemId, int quantidade, decimal pesoMedido, bool identificadoPorCamera)
    {
        GarantirContagemPermitida();
        if (quantidade < 0)
            throw new RegraDeNegocioException("A quantidade de saída não pode ser negativa.");
        if (pesoMedido < 0)
            throw new RegraDeNegocioException("O peso medido não pode ser negativo.");

        var item = ObterItem(itemId);
        item.RegistrarSaida(quantidade);
        item.RegistrarPeso(pesoMedido);
        item.RegistrarIdentificacaoPorCamera(identificadoPorCamera);
    }

    public void RegistrarPeso(Guid itemId, decimal pesoMedido)
    {
        GarantirContagemPermitida();
        if (pesoMedido < 0)
            throw new RegraDeNegocioException("O peso medido não pode ser negativo.");

        ObterItem(itemId).RegistrarPeso(pesoMedido);
    }

    private void GarantirContagemPermitida()
    {
        if (Status != StatusProcedimento.EmAndamento)
            throw new RegraDeNegocioException("Só é possível registrar contagem em procedimento em andamento.");
    }

    private ItemProcedimento ObterItem(Guid itemId)
    {
        return _itens.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Item não encontrado no procedimento.");
    }

    // ===================== Consultas =====================

    // RN4 (via ItemProcedimento.ConferePeso): item com peso fora da tolerância é divergente.
    public List<ItemProcedimento> ObterDiferencas()
    {
        return _itens.Where(i => i.PossuiDivergencia()).ToList();
    }

    public Relatorio GerarRelatorio()
    {
        return new Relatorio
        {
            Procedimento = this,
            TotalInstrumentos = _itens.Sum(i => i.QuantidadeInicial),
            Divergencia = ObterDiferencas().Count > 0
        };
    }
}
