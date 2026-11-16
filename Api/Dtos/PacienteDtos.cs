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
        // Valida e converte tudo ANTES de alterar o paciente: se algo for recusado, nada muda.
        var data = ConverterData(DataNascimento);
        var sexo = ValorDaLista(Sexo, Paciente.SexosValidos, "Sexo");
        var tipoSanguineo = ValorDaLista(TipoSanguineo?.Replace('−', '-'), Paciente.TiposSanguineosValidos, "Tipo sanguíneo");

        paciente.EditarDados(Nome, Alergias ?? "", Observacoes ?? "");
        paciente.DataNascimento = data;
        paciente.Sexo = sexo;
        paciente.TipoSanguineo = tipoSanguineo;
    }

    /// <summary>
    /// Vazio vira "Não informado"; maiúsculas/minúsculas não importam e o valor é gravado
    /// como está na lista. Valor fora da lista é regra violada (HTTP 400).
    /// </summary>
    private static string ValorDaLista(string? valor, IReadOnlyList<string> validos, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return Paciente.NaoInformado;

        return validos.FirstOrDefault(v => string.Equals(v, valor.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new RegraDeNegocioException(
                $"{campo} inválido: '{valor}'. Use um de: {string.Join(", ", validos)}.");
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
