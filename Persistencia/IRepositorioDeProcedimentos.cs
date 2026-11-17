using ScanOPS.Cirurgias;

namespace ScanOPS.Persistencia;

/// <summary>
/// Acesso ao agregado Procedimento. As rotas e os serviços dependem desta interface, não do EF:
/// o procedimento sempre volta completo (itens, instrumentos e pessoas) e as
/// alterações feitas pelos métodos de domínio são gravadas em Salvar().
/// </summary>
public interface IRepositorioDeProcedimentos
{
    /// <summary>Procedimento com início mais recente (rotas antigas /api/procedimento-atual), ou null.</summary>
    Procedimento? ObterAtual();

    Procedimento? ObterPorId(Guid id);

    /// <summary>Procedimentos com algum dos status informados.</summary>
    IReadOnlyList<Procedimento> ListarPorStatus(params StatusProcedimento[] status);

    /// <summary>
    /// Procedimento EmAndamento na sala (sala já padronizada, comparação sem diferenciar maiúsculas),
    /// ignorando o procedimento de id <paramref name="exceto"/>. Base da RN7.
    /// </summary>
    Procedimento? ObterEmAndamentoNaSala(string sala, Guid exceto);

    void Adicionar(Procedimento procedimento);

    /// <summary>Grava as alterações. Violação do índice de sala ocupada vira RegraDeNegocioException.</summary>
    void Salvar();
}
