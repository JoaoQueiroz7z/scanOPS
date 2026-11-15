using ScanOPS.Cirurgias;
using ScanOPS.Instrumentos;

namespace ScanOPS.Tests.Dominio;

/// <summary>
/// Testes de domínio da contagem: o Procedimento só aceita registrar
/// entrada/saída enquanto está EmAndamento e com quantidade válida.
/// </summary>
public class ProcedimentoContagemTests
{
    private static Procedimento CriarEmAndamento()
    {
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m }, 2);
        procedimento.Iniciar();
        return procedimento;
    }

    [Fact]
    public void RegistrarEntrada_EmAndamentoComQuantidadePositiva_SomaNaContagemInicial()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];

        procedimento.RegistrarEntrada(item.Id, 2);

        Assert.Equal(2, item.QuantidadeInicial);
    }

    [Fact]
    public void RegistrarSaida_QuantidadeZero_EhAceita()
    {
        // Saída zero significa "nenhuma unidade voltou" (usado na tela de contagem final).
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];

        procedimento.RegistrarSaida(item.Id, 0, 0m, false);

        Assert.Equal(0, item.QuantidadeFinal);
    }

    [Fact]
    public void RegistrarEntrada_ProcedimentoFinalizado_LancaExcecaoENaoAlteraContagem()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.Finalizar();

        Assert.Throws<InvalidOperationException>(() => procedimento.RegistrarEntrada(item.Id, 3));

        Assert.Equal(0, item.QuantidadeInicial);
        Assert.Equal(StatusProcedimento.Finalizado, procedimento.Status);
    }

    [Fact]
    public void RegistrarSaida_ProcedimentoPlanejado_LancaExcecao()
    {
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Tesoura Mayo", PesoReferencia = 80m }, 1);
        var item = procedimento.Itens[0];

        Assert.Throws<InvalidOperationException>(() => procedimento.RegistrarSaida(item.Id, 1, 80m, true));

        Assert.Equal(0, item.QuantidadeFinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RegistrarEntrada_QuantidadeMenorOuIgualAZero_LancaExcecaoENaoAlteraContagem(int quantidade)
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];

        Assert.Throws<InvalidOperationException>(() => procedimento.RegistrarEntrada(item.Id, quantidade));

        Assert.Equal(0, item.QuantidadeInicial);
    }

    [Fact]
    public void RegistrarSaida_QuantidadeNegativa_LancaExcecao()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];

        Assert.Throws<InvalidOperationException>(() => procedimento.RegistrarSaida(item.Id, -1, 0m, false));

        Assert.Equal(0, item.QuantidadeFinal);
    }

    [Fact]
    public void RegistrarEntrada_ItemInexistente_LancaKeyNotFound()
    {
        var procedimento = CriarEmAndamento();

        Assert.Throws<KeyNotFoundException>(() => procedimento.RegistrarEntrada(Guid.NewGuid(), 1));
    }
}
