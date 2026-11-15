using ScanOPS.Cirurgias;
using ScanOPS.Contagens;

namespace ScanOPS.Api.Dtos;

// ---------- criação e ciclo de vida (rotas /api/procedimentos) ----------

public record ItemPrevistoRequisicao(Guid InstrumentoId, int QuantidadePrevista);

/// <summary>Dados para planejar um procedimento. Ele nasce Planejado; o início é uma ação separada (RN1).</summary>
public record NovoProcedimentoRequisicao(
    string Tipo,
    string Sala,
    string? Descricao,
    Guid PacienteId,
    Guid CirurgiaoId,
    Guid InstrumentistaId,
    IReadOnlyList<ItemPrevistoRequisicao> Itens);

public record CancelarRequisicao(string Motivo);

// ---------- requisições das rotas do procedimento atual ----------

public record EntradaRequisicao(int Quantidade);

public record SaidaRequisicao(int Quantidade, decimal PesoMedido, bool IdentificadoPorCamera);

public record FinalizarRequisicao(string? Justificativa);

// ---------- respostas ----------

public record ItemProcedimentoResposta(
    Guid Id,
    string Instrumento,
    int QuantidadePrevista,
    int QuantidadeInicial,
    int QuantidadeFinal,
    int Diferenca,
    decimal PesoMedido,
    bool IdentificadoPorCamera,
    string Status)
{
    public static ItemProcedimentoResposta De(ItemProcedimento i) => new(
        i.Id, i.Instrumento.Nome, i.QuantidadePrevista, i.QuantidadeInicial, i.QuantidadeFinal,
        i.Diferenca, i.PesoMedido, i.IdentificadoPorCamera, i.Status.ToString());
}

public record ProcedimentoResposta(
    Guid Id,
    string Tipo,
    string Status,
    IReadOnlyList<ItemProcedimentoResposta> Itens)
{
    public static ProcedimentoResposta De(Procedimento p) => new(
        p.Id, p.Tipo, p.Status.ToString(), p.Itens.Select(ItemProcedimentoResposta.De).ToList());
}

public record PesagemResposta(decimal PesoMedido);

public record FinalizacaoResposta(string Status);
