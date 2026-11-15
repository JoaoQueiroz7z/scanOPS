using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScanOPS.Api;
using ScanOPS.Cirurgias;
using ScanOPS.Excecoes;
using ScanOPS.Hardware;
using ScanOPS.Instrumentos;
using ScanOPS.Servicos;
using ScanOPS.Tests.Dubles;

namespace ScanOPS.Tests.Excecoes;

/// <summary>
/// As três situações de falha documentadas em docs/av2 (item D01):
///   F1 (regra)        finalizar com divergência sem justificativa      -> RegraDeNegocioException -> HTTP 400
///   F2 (regra)        registrar contagem com procedimento finalizado   -> RegraDeNegocioException -> HTTP 400
///   F3 (excepcional)  balança indisponível durante a pesagem           -> FalhaDeHardwareException -> HTTP 503
/// Cada situação é testada no domínio (exceção + estado) e na fronteira HTTP (código + mensagem).
/// </summary>
public class SituacoesDeFalhaTests
{
    private const string Api = "/api/procedimento-atual";

    private static Procedimento CriarEmAndamento()
    {
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m }, 2);
        procedimento.Iniciar();
        return procedimento;
    }

    private static async Task<Guid> PrimeiroItemAsync(HttpClient cliente)
    {
        var json = await cliente.GetFromJsonAsync<JsonElement>(Api);
        return json.GetProperty("itens")[0].GetProperty("id").GetGuid();
    }

    private static WebApplicationFactory<Program> ApiComBalanca(IBalanca balanca)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(servicos =>
            {
                servicos.RemoveAll<IBalanca>();
                servicos.AddSingleton(balanca);
            }));
    }

    // ---------- F1: divergência sem justificativa (regra) ----------

    [Fact]
    public void F1_Dominio_FinalizarComDivergenciaSemJustificativa_LancaRegraEContinuaEmAndamento()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 1, 50m, true); // falta 1 instrumento

        var erro = Assert.Throws<RegraDeNegocioException>(() => procedimento.Finalizar());

        Assert.Contains("justificativa", erro.Message);
        Assert.Equal(StatusProcedimento.EmAndamento, procedimento.Status);
        Assert.Null(procedimento.DataHoraFim);
    }

    [Fact]
    public async Task F1_Api_FinalizarComDivergenciaSemJustificativa_Retorna400ComMensagemDaRegra()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        var cliente = fabrica.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);
        await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/entrada", new { quantidade = 2 });
        await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/saida", new { quantidade = 1, pesoMedido = 50m, identificadoPorCamera = true });

        var resposta = await cliente.PostAsJsonAsync($"{Api}/finalizar", new { justificativa = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("justificativa", corpo.GetProperty("erro").GetString());
        var estado = await cliente.GetFromJsonAsync<JsonElement>(Api);
        Assert.Equal("EmAndamento", estado.GetProperty("status").GetString());
    }

    // ---------- F2: contagem após finalizar (regra) ----------

    [Fact]
    public void F2_Dominio_RegistrarSaidaAposFinalizar_LancaRegraENaoAlteraItem()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        procedimento.Finalizar();

        Assert.Throws<RegraDeNegocioException>(() => procedimento.RegistrarSaida(item.Id, 1, 50m, true));

        Assert.Equal(0, item.QuantidadeFinal);
        Assert.Equal(0m, item.PesoMedido);
    }

    [Fact]
    public async Task F2_Api_RegistrarSaidaAposFinalizar_Retorna400ComMensagemDaRegra()
    {
        using var fabrica = new WebApplicationFactory<Program>();
        var cliente = fabrica.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);
        await cliente.PostAsJsonAsync($"{Api}/finalizar", new { justificativa = (string?)null });

        var resposta = await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/saida", new { quantidade = 1, pesoMedido = 50m, identificadoPorCamera = true });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("em andamento", corpo.GetProperty("erro").GetString());
    }

    // ---------- F3: balança indisponível (excepcional) ----------

    [Fact]
    public void F3_Servico_BalancaDesconectada_LancaFalhaDeHardwareENaoAlteraPeso()
    {
        var procedimento = CriarEmAndamento();
        var item = procedimento.Itens[0];
        var balanca = new BalancaDesconectada();
        var servico = new ServicoDePesagem(balanca);

        Assert.Throws<FalhaDeHardwareException>(() => servico.PesarItem(procedimento, item.Id));

        Assert.False(balanca.PesarFoiChamado);
        Assert.Equal(0m, item.PesoMedido);
    }

    [Fact]
    public void F3_Servico_ErroDoDriver_EhEmbrulhadoEmFalhaDeHardwarePreservandoACausa()
    {
        var procedimento = CriarEmAndamento();
        var servico = new ServicoDePesagem(new BalancaComErroDeLeitura());

        var erro = Assert.Throws<FalhaDeHardwareException>(() => servico.PesarItem(procedimento, procedimento.Itens[0].Id));

        Assert.IsType<IOException>(erro.InnerException);
    }

    [Fact]
    public async Task F3_Api_ErroNaBalanca_Retorna503ComMensagemGenericaSemDetalhesInternos()
    {
        using var fabrica = ApiComBalanca(new BalancaComErroDeLeitura());
        var cliente = fabrica.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);

        var resposta = await cliente.PostAsync($"{Api}/itens/{itemId}/pesar", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        var texto = await resposta.Content.ReadAsStringAsync();
        var corpo = JsonDocument.Parse(texto).RootElement;
        Assert.Equal(TratadorDeExcecoes.MensagemHardwareIndisponivel, corpo.GetProperty("erro").GetString());
        // Nada interno vaza para o usuário: nem a mensagem do driver, nem tipo de exceção, nem stack trace.
        Assert.DoesNotContain("COM3", texto);
        Assert.DoesNotContain("Exception", texto);
        Assert.DoesNotContain(" at ", texto);
    }

    // ---------- caminho de sucesso da pesagem (contraste com F3) ----------

    [Fact]
    public async Task Api_PesarComBalancaFuncionando_Retorna200EGravaPesoNoItem()
    {
        var balanca = new BalancaSimulada();
        balanca.DefinirPeso(100m);
        using var fabrica = ApiComBalanca(balanca);
        var cliente = fabrica.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);

        var resposta = await cliente.PostAsync($"{Api}/itens/{itemId}/pesar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await cliente.GetFromJsonAsync<JsonElement>(Api);
        Assert.Equal(100m, estado.GetProperty("itens")[0].GetProperty("pesoMedido").GetDecimal());
    }
}
