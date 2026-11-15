using ScanOPS.Contagens;
using ScanOPS.Instrumentos;
using ScanOPS.Pessoas;
using ScanOPS.Relatorios;

namespace ScanOPS.Cirurgias;

public class Procedimento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Tipo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Sala { get; set; } = "";
    public DateTime? DataHoraInicio { get; private set; }
    public DateTime? DataHoraFim { get; private set; }
    public StatusProcedimento Status { get; private set; } = StatusProcedimento.Planejado;
    public string Observacoes { get; set; } = "";

    public Paciente Paciente { get; set; } = null!;
    public Usuario Cirurgiao { get; set; } = null!;
    public Usuario Instrumentista { get; set; } = null!;

    public List<ItemProcedimento> Itens { get; } = new();

    public void Iniciar()
    {
        if (Status != StatusProcedimento.Planejado)
            throw new InvalidOperationException("Só é possível iniciar um procedimento planejado.");

        Status = StatusProcedimento.EmAndamento;
        DataHoraInicio = DateTime.Now;
    }

    public void Finalizar(string? justificativa = null)
    {
        if (Status != StatusProcedimento.EmAndamento)
            throw new InvalidOperationException("Só é possível finalizar um procedimento em andamento.");

        foreach (var item in Itens)
            item.Conferir();

        if (ObterDiferencas().Count > 0)
        {
            if (string.IsNullOrWhiteSpace(justificativa))
                throw new InvalidOperationException(
                    "Há divergências na contagem. Informe uma justificativa para finalizar.");

            Observacoes = $"{Observacoes}\n[Justificativa] {justificativa}".Trim();
        }

        Status = StatusProcedimento.Finalizado;
        DataHoraFim = DateTime.Now;
    }

    public void AdicionarInstrumento(Instrumento instr, int quantidadePrevista = 1)
    {
        Itens.Add(new ItemProcedimento
        {
            Instrumento = instr,
            QuantidadePrevista = quantidadePrevista
        });
    }

    // A contagem passa pelo Procedimento porque só ele conhece o próprio status:
    // procedimento que não está em andamento não aceita alteração de contagem.
    public void RegistrarEntrada(Guid itemId, int quantidade)
    {
        GarantirContagemPermitida();
        if (quantidade <= 0)
            throw new InvalidOperationException("A quantidade de entrada deve ser maior que zero.");

        ObterItem(itemId).RegistrarEntrada(quantidade);
    }

    public void RegistrarSaida(Guid itemId, int quantidade, decimal pesoMedido, bool identificadoPorCamera)
    {
        GarantirContagemPermitida();
        if (quantidade < 0)
            throw new InvalidOperationException("A quantidade de saída não pode ser negativa.");

        var item = ObterItem(itemId);
        item.RegistrarSaida(quantidade);
        item.PesoMedido = pesoMedido;
        item.IdentificadoPorCamera = identificadoPorCamera;
    }

    private void GarantirContagemPermitida()
    {
        if (Status != StatusProcedimento.EmAndamento)
            throw new InvalidOperationException("Só é possível registrar contagem em procedimento em andamento.");
    }

    private ItemProcedimento ObterItem(Guid itemId)
    {
        return Itens.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Item não encontrado no procedimento.");
    }

    public List<ItemProcedimento> ObterDiferencas()
    {
        return Itens
            .Where(i => i.CalcularDiferenca() != 0 || !i.ConferePeso())
            .ToList();
    }

    public Relatorio GerarRelatorio()
    {
        return new Relatorio
        {
            Procedimento = this,
            TotalInstrumentos = Itens.Sum(i => i.QuantidadeInicial),
            Divergencia = ObterDiferencas().Count > 0
        };
    }
}