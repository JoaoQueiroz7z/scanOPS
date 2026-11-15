namespace ScanOPS.Pessoas;

public class Paciente
{
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