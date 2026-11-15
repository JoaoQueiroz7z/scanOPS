using System.Reflection;
using ScanOPS.Cirurgias;
using ScanOPS.Contagens;
using ScanOPS.Excecoes;
using ScanOPS.Instrumentos;

namespace ScanOPS.Tests.Regras;

/// <summary>
/// Invariante do agregado: "Procedimento Finalizado não aceita alterações de
/// contagem; só o Procedimento altera Status e itens; operação recusada não deixa
/// estado parcial". Também audita o encapsulamento que garante essa invariante.
/// </summary>
public class InvarianteProcedimentoTests
{
    [Fact]
    public void FinalizarRecusadoPorDivergencia_NaoAlteraNenhumEstado()
    {
        // Antes da correção do passo 4, Finalizar marcava os itens como Divergente
        // e só depois recusava, deixando o objeto num estado intermediário.
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m }, 2);
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Tesoura Mayo", PesoReferencia = 80m }, 1);
        procedimento.Iniciar();
        var pinca = procedimento.Itens[0];
        var tesoura = procedimento.Itens[1];
        procedimento.RegistrarEntrada(pinca.Id, 2);
        procedimento.RegistrarSaida(pinca.Id, 1, 50m, true); // divergente
        procedimento.RegistrarEntrada(tesoura.Id, 1);
        procedimento.RegistrarSaida(tesoura.Id, 1, 80m, true); // ok

        Assert.Throws<RegraDeNegocioException>(() => procedimento.Finalizar());

        Assert.Equal(StatusProcedimento.EmAndamento, procedimento.Status);
        Assert.Null(procedimento.DataHoraFim);
        Assert.Equal("", procedimento.Observacoes);
        Assert.All(procedimento.Itens, i => Assert.Equal(StatusItem.Pendente, i.Status));
        // E o objeto continua utilizável: com justificativa, a finalização funciona.
        procedimento.Finalizar("Pinça localizada na mesa auxiliar.");
        Assert.Equal(StatusProcedimento.Finalizado, procedimento.Status);
    }

    [Fact]
    public void ProcedimentoFinalizado_RecusaTodaAlteracaoEPermaneceIntacto()
    {
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m }, 2);
        procedimento.Iniciar();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, 100m, true);
        procedimento.Finalizar();

        Assert.Throws<RegraDeNegocioException>(() => procedimento.RegistrarEntrada(item.Id, 1));
        Assert.Throws<RegraDeNegocioException>(() => procedimento.RegistrarSaida(item.Id, 1, 50m, true));
        Assert.Throws<RegraDeNegocioException>(() => procedimento.RegistrarPeso(item.Id, 0m));
        Assert.Throws<RegraDeNegocioException>(() => procedimento.AdicionarInstrumento(new Instrumento(), 1));
        Assert.Throws<RegraDeNegocioException>(() => procedimento.Iniciar());
        Assert.Throws<RegraDeNegocioException>(() => procedimento.Cancelar("tentativa"));

        Assert.Equal(StatusProcedimento.Finalizado, procedimento.Status);
        Assert.Equal(2, item.QuantidadeInicial);
        Assert.Equal(2, item.QuantidadeFinal);
        Assert.Equal(100m, item.PesoMedido);
        Assert.Equal(StatusItem.Conferido, item.Status);
        Assert.Single(procedimento.Itens);
    }

    // ---------- auditoria de encapsulamento (o compilador impede o desvio) ----------

    [Theory]
    [InlineData(nameof(ItemProcedimento.QuantidadeInicial))]
    [InlineData(nameof(ItemProcedimento.QuantidadeFinal))]
    [InlineData(nameof(ItemProcedimento.PesoMedido))]
    [InlineData(nameof(ItemProcedimento.IdentificadoPorCamera))]
    [InlineData(nameof(ItemProcedimento.Status))]
    public void ItemProcedimento_PropriedadesDeContagemNaoTemSetterPublico(string propriedade)
    {
        var setter = typeof(ItemProcedimento).GetProperty(propriedade)!.GetSetMethod(nonPublic: false);

        Assert.Null(setter);
    }

    [Fact]
    public void ItemProcedimento_MetodosDeAlteracaoEConstrutorNaoSaoPublicos()
    {
        var tipo = typeof(ItemProcedimento);
        var publicos = BindingFlags.Public | BindingFlags.Instance;

        Assert.Null(tipo.GetMethod("RegistrarEntrada", publicos));
        Assert.Null(tipo.GetMethod("RegistrarSaida", publicos));
        Assert.Null(tipo.GetMethod("RegistrarPeso", publicos));
        Assert.Null(tipo.GetMethod("Conferir", publicos));
        Assert.Empty(tipo.GetConstructors(publicos));
    }

    [Fact]
    public void Procedimento_StatusEItensNaoPodemSerAlteradosDeFora()
    {
        var tipo = typeof(Procedimento);

        Assert.Null(tipo.GetProperty(nameof(Procedimento.Status))!.GetSetMethod(nonPublic: false));
        Assert.Null(tipo.GetProperty(nameof(Procedimento.Observacoes))!.GetSetMethod(nonPublic: false));
        // A lista exposta é somente leitura: não é List<T>, então não há Add/Remove/Clear públicos.
        Assert.Equal(typeof(IReadOnlyList<ItemProcedimento>), tipo.GetProperty(nameof(Procedimento.Itens))!.PropertyType);
    }
}
