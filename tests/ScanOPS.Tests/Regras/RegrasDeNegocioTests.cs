using ScanOPS.Cirurgias;
using ScanOPS.Contagens;
using ScanOPS.Excecoes;
using ScanOPS.Instrumentos;

namespace ScanOPS.Tests.Regras;

/// <summary>
/// Um cenário de sucesso e pelo menos um de violação para cada regra RN1..RN5
/// (ver docs/av2/especificacao.md e docs/av2/rastreabilidade.md).
/// </summary>
public class RegrasDeNegocioTests
{
    private static readonly Instrumento Pinca = new() { Nome = "Pinça Kelly", PesoReferencia = 50m };

    private static Procedimento CriarPlanejado()
    {
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(Pinca, 2);
        return procedimento;
    }

    private static Procedimento CriarEmAndamento()
    {
        var procedimento = CriarPlanejado();
        procedimento.Iniciar();
        return procedimento;
    }

    /// <summary>Entrada de 2 pinças e saída de 2 com peso exato: sem divergência.</summary>
    private static Procedimento CriarEmAndamentoSemDivergencia()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, 100m, true);
        return procedimento;
    }

    // ===================== RN1: iniciar só se Planejado =====================

    [Fact]
    public void RN1_Sucesso_IniciarPlanejado_FicaEmAndamento()
    {
        var procedimento = CriarPlanejado();

        procedimento.Iniciar();

        Assert.Equal(StatusProcedimento.EmAndamento, procedimento.Status);
        Assert.NotNull(procedimento.DataHoraInicio);
    }

    [Fact]
    public void RN1_Violacao_IniciarJaEmAndamento_LancaEMantemDataDeInicio()
    {
        var procedimento = CriarEmAndamento();
        var inicioOriginal = procedimento.DataHoraInicio;

        Assert.Throws<RegraDeNegocioException>(() => procedimento.Iniciar());

        Assert.Equal(StatusProcedimento.EmAndamento, procedimento.Status);
        Assert.Equal(inicioOriginal, procedimento.DataHoraInicio);
    }

    [Fact]
    public void RN1_Violacao_IniciarCancelado_Lanca()
    {
        var procedimento = CriarPlanejado();
        procedimento.Cancelar("Paciente transferido.");

        Assert.Throws<RegraDeNegocioException>(() => procedimento.Iniciar());
        Assert.Equal(StatusProcedimento.Cancelado, procedimento.Status);
    }

    // ===================== RN2: finalizar só se EmAndamento =====================

    [Fact]
    public void RN2_Sucesso_FinalizarEmAndamentoSemDivergencia_FicaFinalizado()
    {
        var procedimento = CriarEmAndamentoSemDivergencia();

        procedimento.Finalizar();

        Assert.Equal(StatusProcedimento.Finalizado, procedimento.Status);
        Assert.NotNull(procedimento.DataHoraFim);
        Assert.Equal(StatusItem.Conferido, procedimento.Itens[0].Status);
    }

    [Fact]
    public void RN2_Violacao_FinalizarPlanejado_LancaEMantemPlanejado()
    {
        var procedimento = CriarPlanejado();

        Assert.Throws<RegraDeNegocioException>(() => procedimento.Finalizar());

        Assert.Equal(StatusProcedimento.Planejado, procedimento.Status);
        Assert.Null(procedimento.DataHoraFim);
    }

    [Fact]
    public void RN2_Violacao_FinalizarDuasVezes_LancaEMantemDataDeFim()
    {
        var procedimento = CriarEmAndamentoSemDivergencia();
        procedimento.Finalizar();
        var fimOriginal = procedimento.DataHoraFim;

        Assert.Throws<RegraDeNegocioException>(() => procedimento.Finalizar());

        Assert.Equal(fimOriginal, procedimento.DataHoraFim);
    }

    // ===================== RN3: divergência exige justificativa =====================

    [Fact]
    public void RN3_Sucesso_DivergenciaComJustificativa_FinalizaERegistraJustificativa()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 1, 50m, true);

        procedimento.Finalizar("Pinça localizada na mesa auxiliar.");

        Assert.Equal(StatusProcedimento.Finalizado, procedimento.Status);
        Assert.Contains("[Justificativa] Pinça localizada na mesa auxiliar.", procedimento.Observacoes);
        Assert.Equal(StatusItem.Divergente, item.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RN3_Violacao_DivergenciaSemJustificativa_Lanca(string? justificativa)
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 1, 50m, true);

        var erro = Assert.Throws<RegraDeNegocioException>(() => procedimento.Finalizar(justificativa));

        Assert.Contains("justificativa", erro.Message);
        Assert.Equal(StatusProcedimento.EmAndamento, procedimento.Status);
    }

    // ===================== RN4: peso conferido com tolerância =====================

    [Theory]
    [InlineData(100)]   // exato
    [InlineData(102)]   // no limite superior da tolerância (2 g)
    [InlineData(98)]    // no limite inferior
    public void RN4_Sucesso_PesoDentroDaTolerancia_ItemSemDivergencia(decimal pesoMedido)
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, pesoMedido, true);

        Assert.True(item.ConferePeso());
        procedimento.Finalizar(); // sem justificativa: não há divergência
        Assert.Equal(StatusItem.Conferido, item.Status);
    }

    [Theory]
    [InlineData(102.01)] // logo acima da tolerância
    [InlineData(50)]     // peso de uma pinça só, apesar da contagem dizer 2
    public void RN4_Violacao_PesoForaDaTolerancia_ItemDivergenteEFinalizarExigeJustificativa(decimal pesoMedido)
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, pesoMedido, true); // contagem bate, peso não

        Assert.False(item.ConferePeso());
        Assert.Contains(item, procedimento.ObterDiferencas());
        Assert.Throws<RegraDeNegocioException>(() => procedimento.Finalizar());
    }

    // ===================== RN5: contagem só EmAndamento e com quantidade válida =====================

    [Fact]
    public void RN5_Sucesso_EntradaESaidaEmAndamento_AtualizamContagem()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];

        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, 100m, true);

        Assert.Equal(2, item.QuantidadeInicial);
        Assert.Equal(2, item.QuantidadeFinal);
        Assert.Equal(0, item.Diferenca);
    }

    [Fact]
    public void RN5_Violacao_EntradaComProcedimentoPlanejado_Lanca()
    {
        var procedimento = CriarPlanejado();
        var item = procedimento.Itens[0];

        Assert.Throws<RegraDeNegocioException>(() => procedimento.RegistrarEntrada(item.Id, 1));
        Assert.Equal(0, item.QuantidadeInicial);
    }

    [Fact]
    public void RN5_Violacao_PesoNegativoNaSaida_LancaENaoAlteraNada()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];

        Assert.Throws<RegraDeNegocioException>(() => procedimento.RegistrarSaida(item.Id, 1, -10m, true));

        Assert.Equal(0, item.QuantidadeFinal);
        Assert.Equal(0m, item.PesoMedido);
        Assert.False(item.IdentificadoPorCamera);
    }

    // ===================== Cancelamento e composição =====================

    [Fact]
    public void Cancelar_Planejado_FicaCanceladoERegistraMotivo()
    {
        var procedimento = CriarPlanejado();

        procedimento.Cancelar("Paciente transferido.");

        Assert.Equal(StatusProcedimento.Cancelado, procedimento.Status);
        Assert.Contains("[Cancelamento] Paciente transferido.", procedimento.Observacoes);
    }

    [Fact]
    public void Cancelar_EmAndamento_LancaPoisInstrumentosJaEstaoEmCampo()
    {
        var procedimento = CriarEmAndamento();

        Assert.Throws<RegraDeNegocioException>(() => procedimento.Cancelar("Desistência."));
        Assert.Equal(StatusProcedimento.EmAndamento, procedimento.Status);
    }

    [Fact]
    public void Cancelar_SemMotivo_LancaEContinuaPlanejado()
    {
        var procedimento = CriarPlanejado();

        Assert.Throws<RegraDeNegocioException>(() => procedimento.Cancelar(" "));
        Assert.Equal(StatusProcedimento.Planejado, procedimento.Status);
    }

    [Fact]
    public void AdicionarInstrumento_DepoisDeIniciado_LancaENaoAlteraLista()
    {
        var procedimento = CriarEmAndamento();

        Assert.Throws<RegraDeNegocioException>(() => procedimento.AdicionarInstrumento(Pinca, 1));
        Assert.Single(procedimento.Itens);
    }

    [Fact]
    public void Identidade_TransicoesPreservamOMesmoId()
    {
        var procedimento = CriarPlanejado();
        var id = procedimento.Id;

        procedimento.Iniciar();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, 100m, true);
        procedimento.Finalizar();

        Assert.Equal(id, procedimento.Id);
    }
}
