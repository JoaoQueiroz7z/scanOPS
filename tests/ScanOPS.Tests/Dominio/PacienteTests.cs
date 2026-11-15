using ScanOPS.Pessoas;

namespace ScanOPS.Tests.Dominio;

public class PacienteTests
{
    [Fact]
    public void GetIdade_SemDataDeNascimento_RetornaNulo()
    {
        var paciente = new Paciente { Nome = "Maria" };

        Assert.Null(paciente.GetIdade());
    }

    [Fact]
    public void GetIdade_AniversarioAindaNaoChegouEsteAno_DescontaUmAno()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var paciente = new Paciente { DataNascimento = hoje.AddYears(-40).AddDays(1) };

        Assert.Equal(39, paciente.GetIdade());
    }

    [Fact]
    public void GetIdade_AniversarioHoje_ContaOAnoCompleto()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var paciente = new Paciente { DataNascimento = hoje.AddYears(-40) };

        Assert.Equal(40, paciente.GetIdade());
    }
}
