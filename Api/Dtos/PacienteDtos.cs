using System.Globalization;
using ScanOPS.Excecoes;
using ScanOPS.Pessoas;

namespace ScanOPS.Api.Dtos;

/// <summary>
/// O que o front envia para criar ou editar um paciente. Não tem Id.
/// DataNascimento chega como texto "aaaa-mm-dd" (campo de data do formulário);
/// vazio significa "não informada".
/// </summary>
public record PacienteRequisicao(
    string Nome,
    string? DataNascimento,
    string Sexo,
    string TipoSanguineo,
    string Alergias,
    string Observacoes)
{
    public Paciente CriarPaciente()
    {
        var paciente = new Paciente();
        AplicarEm(paciente);
        return paciente;
    }

    public void AplicarEm(Paciente paciente)
    {
        paciente.EditarDados(Nome, Alergias ?? "", Observacoes ?? "");
        paciente.DataNascimento = ConverterData(DataNascimento);
        paciente.Sexo = Sexo ?? "";
        paciente.TipoSanguineo = TipoSanguineo ?? "";
    }

    private static DateOnly? ConverterData(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        if (DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var convertida))
            return convertida;

        throw new RegraDeNegocioException($"Data de nascimento inválida: '{data}'. Use o formato aaaa-mm-dd.");
    }
}

/// <summary>O que a API devolve sobre um paciente, incluindo a idade calculada pelo domínio.</summary>
public record PacienteResposta(
    Guid Id,
    string Nome,
    DateOnly? DataNascimento,
    int? Idade,
    string Sexo,
    string TipoSanguineo,
    string Alergias,
    string Observacoes)
{
    public static PacienteResposta De(Paciente p) => new(
        p.Id, p.Nome, p.DataNascimento, p.GetIdade(), p.Sexo, p.TipoSanguineo, p.Alergias, p.Observacoes);
}
