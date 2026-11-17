using ScanOPS.Cirurgias;

namespace ScanOPS.Api.Dtos;

/// <summary>
/// Detalhe completo de um procedimento (GET /api/procedimentos/{id}), usado no histórico.
/// Os itens mostram o nome e o peso de referência da "foto" tirada no planejamento.
/// </summary>
public record ProcedimentoDetalheResposta(
    Guid Id,
    string Tipo,
    string Status,
    string Sala,
    string Descricao,
    string Paciente,
    string Cirurgiao,
    string Instrumentista,
    DateTime? DataHoraInicio,
    DateTime? DataHoraFim,
    int? DuracaoMinutos,
    string? Justificativa,
    string? MotivoCancelamento,
    IReadOnlyList<ItemProcedimentoResposta> Itens)
{
    public static ProcedimentoDetalheResposta De(Procedimento p) => new(
        p.Id, p.Tipo, p.Status.ToString(), p.Sala, p.Descricao,
        p.Paciente?.Nome ?? "", p.Cirurgiao?.Nome ?? "", p.Instrumentista?.Nome ?? "",
        p.DataHoraInicio, p.DataHoraFim, Duracao.EmMinutos(p),
        p.Justificativa, p.MotivoCancelamento,
        p.Itens.Select(ItemProcedimentoResposta.De).ToList());
}

/// <summary>Linha resumida das listas de histórico e de planejados.</summary>
public record ProcedimentoResumoResposta(
    Guid Id,
    string Tipo,
    string Status,
    string Sala,
    string Paciente,
    string Cirurgiao,
    DateTime? DataHoraInicio,
    DateTime? DataHoraFim,
    int? DuracaoMinutos,
    bool TeveDivergencia)
{
    public static ProcedimentoResumoResposta De(Procedimento p) => new(
        p.Id, p.Tipo, p.Status.ToString(), p.Sala, p.Paciente?.Nome ?? "", p.Cirurgiao?.Nome ?? "",
        p.DataHoraInicio, p.DataHoraFim, Duracao.EmMinutos(p),
        p.Status == StatusProcedimento.Finalizado && p.Justificativa is not null);
}

/// <summary>Uma sala ocupada (GET /api/salas/em-uso): o procedimento EmAndamento nela.</summary>
public record SalaEmUsoResposta(
    string Sala,
    Guid ProcedimentoId,
    string Tipo,
    string Paciente,
    string Cirurgiao,
    DateTime? DataHoraInicio)
{
    public static SalaEmUsoResposta De(Procedimento p) => new(
        p.Sala, p.Id, p.Tipo, p.Paciente?.Nome ?? "", p.Cirurgiao?.Nome ?? "", p.DataHoraInicio);
}

internal static class Duracao
{
    /// <summary>Minutos entre início e fim; null se não começou ou não terminou (ex.: cancelado antes de iniciar).</summary>
    public static int? EmMinutos(Procedimento p) =>
        p.DataHoraInicio is DateTime inicio && p.DataHoraFim is DateTime fim
            ? (int)Math.Round((fim - inicio).TotalMinutes)
            : null;
}
