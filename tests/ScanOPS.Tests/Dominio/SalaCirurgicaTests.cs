using ScanOPS.Cirurgias;
using ScanOPS.Excecoes;

namespace ScanOPS.Tests.Dominio;

public class SalaCirurgicaTests
{
    [Theory]
    [InlineData("Sala 1", "Sala 1")]
    [InlineData("sala 1", "Sala 1")]
    [InlineData("1", "Sala 1")]
    [InlineData("  SALA   01 ", "Sala 1")]
    [InlineData("02", "Sala 2")]
    [InlineData("Sala Híbrida", "Sala Híbrida")]
    [InlineData("0", "Sala 0")]
    public void Padronizar_DiferentesFormas_GravaComoSalaX(string digitado, string esperado)
    {
        Assert.Equal(esperado, SalaCirurgica.Padronizar(digitado));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Sala")]
    [InlineData(null)]
    public void Padronizar_SemNome_Recusa(string? digitado)
    {
        Assert.Throws<RegraDeNegocioException>(() => SalaCirurgica.Padronizar(digitado));
    }

    [Fact]
    public void MesmaSala_IgnoraMaiusculasEspacosEPrefixo()
    {
        Assert.True(SalaCirurgica.MesmaSala("Sala UTI", "uti"));
        Assert.False(SalaCirurgica.MesmaSala("Sala 1", "Sala 10"));
    }
}
