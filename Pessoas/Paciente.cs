namespace ScanOPS.Pessoas;

public class Paciente
{
    public const string NaoInformado = "Não informado";

    /// <summary>Valores aceitos para Sexo (a API recusa qualquer outro).</summary>
    public static readonly IReadOnlyList<string> SexosValidos = ["Masculino", "Feminino", NaoInformado];

    /// <summary>Valores aceitos para TipoSanguineo. Negativos com hífen comum: "A-" (a tela mostra "A−").</summary>
    public static readonly IReadOnlyList<string> TiposSanguineosValidos =
        ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-", NaoInformado];

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = "";
    // Opcional: o paciente pode ser cadastrado antes de a data ser informada.
    public DateOnly? DataNascimento { get; set; }
    public string Sexo { get; set; } = "";
    public string TipoSanguineo { get; set; } = "";
    public string Alergias { get; set; } = "";
    public string Foto { get; set; } = "";
    public string Observacoes { get; set; } = "";

    public void EditarDados(string nome, string alergias, string observacoes)
    {
        Nome = nome;
        Alergias = alergias;
        Observacoes = observacoes;
    }

    /// <summary>Idade em anos completos, ou null se a data de nascimento não foi informada.</summary>
    public int? GetIdade()
    {
        if (DataNascimento is not DateOnly nascimento)
            return null;

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        int idade = hoje.Year - nascimento.Year;
        if (nascimento > hoje.AddYears(-idade)) idade--;
        return idade;
    }
}