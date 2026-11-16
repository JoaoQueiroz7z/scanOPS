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

/// <summary>
/// Um item com a conferência calculada pelo domínio. Status só muda na finalização
/// (Pendente -> Conferido/Divergente); PesoConfere e PossuiDivergencia valem a qualquer momento,
/// para a tela mostrar a divergência antes de finalizar.
/// </summary>
public record ItemProcedimentoResposta(
    Guid Id,
    string Instrumento,
    int QuantidadePrevista,
    int QuantidadeInicial,
    int QuantidadeFinal,
    int Diferenca,
    decimal PesoMedido,
    decimal PesoReferencia,
    decimal PesoEsperado,
    decimal ToleranciaPeso,
    bool PesoConfere,
    bool PossuiDivergencia,
    bool IdentificadoPorCamera,
    string Status)
{
    public static ItemProcedimentoResposta De(ItemProcedimento i) => new(
        i.Id, i.Instrumento.Nome, i.QuantidadePrevista, i.QuantidadeInicial, i.QuantidadeFinal,
        i.Diferenca, i.PesoMedido, i.Instrumento.PesoReferencia, i.PesoEsperado(), i.ToleranciaPeso,
        i.ConferePeso(), i.PossuiDivergencia(), i.IdentificadoPorCamera, i.Status.ToString());
}

/// <summary>
/// Dados do procedimento para as telas. Paciente e Cirurgiao vêm só com o nome
/// (a tela não precisa do cadastro completo de pessoas).
/// </summary>
public record ProcedimentoResposta(
    Guid Id,
    string Tipo,
    string Status,
    string Sala,
    string Paciente,
    string Cirurgiao,
    IReadOnlyList<ItemProcedimentoResposta> Itens)
{
    public static ProcedimentoResposta De(Procedimento p) => new(
        p.Id, p.Tipo, p.Status.ToString(), p.Sala,
        p.Paciente?.Nome ?? "", p.Cirurgiao?.Nome ?? "",
        p.Itens.Select(ItemProcedimentoResposta.De).ToList());
}

public record PesagemResposta(decimal PesoMedido);

public record FinalizacaoResposta(string Status);
