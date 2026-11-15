using ScanOPS.Excecoes;
using ScanOPS.Instrumentos;

namespace ScanOPS.Api.Dtos;

/// <summary>
/// O que o front envia para criar ou editar um instrumento. Não tem Id nem Ativo:
/// o Id é gerado pelo sistema e a inativação é uma operação própria do domínio.
/// Tipo chega como texto ("Pinca", "Tesoura"...), que é o que o formulário envia.
/// </summary>
public record InstrumentoRequisicao(
    string Nome,
    string Codigo,
    string Tipo,
    string Material,
    string Tamanho,
    decimal PesoReferencia)
{
    public Instrumento CriarInstrumento()
    {
        var instrumento = new Instrumento();
        AplicarEm(instrumento);
        return instrumento;
    }

    public void AplicarEm(Instrumento instrumento)
    {
        instrumento.Editar(Nome, Material, Tamanho);
        instrumento.Codigo = Codigo;
        instrumento.Tipo = ConverterTipo(Tipo);
        instrumento.PesoReferencia = PesoReferencia;
    }

    private static TipoInstrumento ConverterTipo(string tipo)
    {
        if (Enum.TryParse<TipoInstrumento>(tipo, ignoreCase: true, out var convertido)
            && Enum.IsDefined(convertido))
            return convertido;

        throw new RegraDeNegocioException(
            $"Tipo de instrumento inválido: '{tipo}'. Use um de: {string.Join(", ", Enum.GetNames<TipoInstrumento>())}.");
    }
}

/// <summary>O que a API devolve sobre um instrumento. Tipo sai como texto.</summary>
public record InstrumentoResposta(
    Guid Id,
    string Nome,
    string Codigo,
    string Tipo,
    string Material,
    string Tamanho,
    string Esterilizacao,
    bool Ativo,
    decimal PesoReferencia)
{
    public static InstrumentoResposta De(Instrumento i) => new(
        i.Id, i.Nome, i.Codigo, i.Tipo.ToString(), i.Material, i.Tamanho, i.Esterilizacao, i.Ativo, i.PesoReferencia);
}
