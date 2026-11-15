using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ScanOPS.Cirurgias;
using ScanOPS.Hardware;
using ScanOPS.Instrumentos;
using ScanOPS.Servicos;

namespace ScanOPS.Tests.Hardware;

/// <summary>
/// Polimorfismo: o contrato IBalanca tem duas implementações com comportamentos
/// diferentes, e o ServicoDePesagem (usado pela rota /pesar) funciona com as duas
/// sem saber qual recebeu. A implementação é escolhida por configuração.
/// </summary>
public class PolimorfismoBalancaTests
{
    private const decimal PesoDeDuasPincas = 100m;

    private static (Procedimento procedimento, Guid itemId) ProcedimentoComDuasPincasDevolvidas()
    {
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m }, 2);
        procedimento.Iniciar();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, 0m, true);
        return (procedimento, item.Id);
    }

    public static TheoryData<string, IBalanca> BalancasComDuasPincasNoPrato()
    {
        var simulada = new BalancaSimulada();
        simulada.DefinirPeso(PesoDeDuasPincas);

        var comRuido = new BalancaComRuido(amplitudeRuido: 1.5m, semente: 42);
        comRuido.DefinirPesoNoPrato(PesoDeDuasPincas);

        return new TheoryData<string, IBalanca>
        {
            { nameof(BalancaSimulada), simulada },
            { nameof(BalancaComRuido), comRuido }
        };
    }

    [Theory]
    [MemberData(nameof(BalancasComDuasPincasNoPrato))]
    public void MesmoServico_ComQualquerBalanca_GravaPesoQueRespeitaRN4(string nome, IBalanca balanca)
    {
        var (procedimento, itemId) = ProcedimentoComDuasPincasDevolvidas();
        var servico = new ServicoDePesagem(balanca);

        var peso = servico.PesarItem(procedimento, itemId);

        var item = procedimento.Itens[0];
        Assert.Equal(peso, item.PesoMedido);
        Assert.True(item.ConferePeso(), $"{nome}: leitura {peso} g deveria estar dentro da tolerância da RN4");
        procedimento.Finalizar(); // sem divergência, não precisa de justificativa
    }

    [Fact]
    public void BalancaSimulada_DevolveSempreOPesoExato()
    {
        var balanca = new BalancaSimulada();
        balanca.DefinirPeso(PesoDeDuasPincas);

        var leituras = Enumerable.Range(0, 5).Select(_ => balanca.Pesar()).ToList();

        Assert.All(leituras, l => Assert.Equal(PesoDeDuasPincas, l));
    }

    [Fact]
    public void BalancaComRuido_LeiturasVariamDentroDaAmplitude()
    {
        var balanca = new BalancaComRuido(amplitudeRuido: 1.5m, semente: 7);
        balanca.DefinirPesoNoPrato(PesoDeDuasPincas);

        var leituras = Enumerable.Range(0, 5).Select(_ =>
        {
            balanca.Calibrar(); // isola o ruído da deriva
            return balanca.Pesar();
        }).ToList();

        Assert.All(leituras, l => Assert.InRange(l, PesoDeDuasPincas - 1.5m, PesoDeDuasPincas + 1.5m));
        Assert.True(leituras.Distinct().Count() > 1, "uma balança com ruído não deveria repetir sempre a mesma leitura");
    }

    [Fact]
    public void BalancaComRuido_DerivaCresceComOUsoECalibrarZera()
    {
        var balanca = new BalancaComRuido(amplitudeRuido: 0m); // sem ruído: isola a deriva
        balanca.DefinirPesoNoPrato(PesoDeDuasPincas);

        for (var i = 0; i < 40; i++) balanca.Pesar();
        var leituraComDeriva = balanca.Pesar();
        balanca.Calibrar();
        var leituraCalibrada = balanca.Pesar();

        Assert.Equal(PesoDeDuasPincas + 40 * BalancaComRuido.DerivaPorLeitura, leituraComDeriva); // 102,0 g
        Assert.Equal(PesoDeDuasPincas, leituraCalibrada);
    }

    [Fact]
    public void BalancaComRuido_PratoVazioNuncaMostraPesoNegativo()
    {
        var balanca = new BalancaComRuido(amplitudeRuido: 1.5m, semente: 1);

        var leituras = Enumerable.Range(0, 20).Select(_ => balanca.Pesar());

        Assert.All(leituras, l => Assert.True(l >= 0m));
    }

    // ---------- escolha da implementação por configuração ----------

    private static IBalanca BalancaRegistradaNaApi(string? tipo)
    {
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            if (tipo is not null) b.UseSetting("Balanca:Tipo", tipo);
        });
        return fabrica.Services.GetRequiredService<IBalanca>();
    }

    [Fact]
    public void Configuracao_Padrao_UsaBalancaSimulada()
    {
        Assert.IsType<BalancaSimulada>(BalancaRegistradaNaApi(null));
    }

    [Fact]
    public void Configuracao_ComRuido_UsaBalancaComRuido()
    {
        Assert.IsType<BalancaComRuido>(BalancaRegistradaNaApi("ComRuido"));
    }

    [Fact]
    public void Configuracao_TipoInvalido_FalhaNaInicializacaoComMensagemClara()
    {
        var erro = Assert.ThrowsAny<Exception>(() => BalancaRegistradaNaApi("Laser"));

        Assert.Contains("Tipo de balança desconhecido", erro.ToString());
    }
}
