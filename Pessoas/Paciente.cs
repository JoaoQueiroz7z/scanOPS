namespace ScanOPS.Pessoas;

public class Paciente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = "";
    public DateOnly DataNascimento { get; set; }
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

    public int GetIdade()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        int idade = hoje.Year - DataNascimento.Year;
        if (DataNascimento > hoje.AddYears(-idade)) idade--;
        return idade;
    }
}