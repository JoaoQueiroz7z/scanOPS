using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScanOPS.Cirurgias;
using ScanOPS.Contagens;
using ScanOPS.Persistencia;
using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>
/// Testes de integração (fatia vertical): HTTP -> rota -> domínio (regras) -> EF Core -> SQLite.
/// A verificação lê o banco com um DbContext NOVO, para provar que o dado foi gravado
/// (e não apenas mantido em memória).
/// </summary>
public class PersistenciaIntegracaoTests
{
    private const string Api = "/api/procedimento-atual";

    private static async Task<Guid> PrimeiroItemAsync(HttpClient cliente)
    {
        var json = await cliente.GetFromJsonAsync<JsonElement>(Api);
        return json.GetProperty("itens")[0].GetProperty("id").GetGuid();
    }

    /// <summary>Lê o procedimento direto do banco, com um contexto novo (sem cache).</summary>
    private static Procedimento LerDoBanco(ApiDeTeste api)
    {
        using var escopo = api.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<ScanOpsDbContext>();
        return db.Procedimentos.AsNoTracking()
            .Include(p => p.Itens.OrderBy(i => i.Sequencia)).ThenInclude(i => i.Instrumento)
            .Single();
    }

    [Fact]
    public async Task ContagemEFinalizacaoComJustificativa_SaoGravadasNoBanco()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);

        // RN5: entrada 2 e saída 1 (falta uma pinça)
        (await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/entrada", new { quantidade = 2 })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/saida", new { quantidade = 1, pesoMedido = 50m, identificadoPorCamera = true })).EnsureSuccessStatusCode();
        // RN3: com divergência, finaliza só com justificativa
        var finalizar = await cliente.PostAsJsonAsync($"{Api}/finalizar", new { justificativa = "Pinça localizada na mesa auxiliar." });
        Assert.Equal(HttpStatusCode.OK, finalizar.StatusCode);

        var gravado = LerDoBanco(api);
        var item = gravado.Itens.Single(i => i.Id == itemId);
        Assert.Equal(StatusProcedimento.Finalizado, gravado.Status);
        Assert.NotNull(gravado.DataHoraFim);
        Assert.Contains("[Justificativa] Pinça localizada na mesa auxiliar.", gravado.Observacoes);
        Assert.Equal(2, item.QuantidadeInicial);
        Assert.Equal(1, item.QuantidadeFinal);
        Assert.Equal(50m, item.PesoMedido);
        Assert.Equal(StatusItem.Divergente, item.Status);
    }

    [Fact]
    public async Task FinalizacaoRecusada_NaoGravaNenhumaAlteracaoNoBanco()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);
        await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/entrada", new { quantidade = 2 });
        await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/saida", new { quantidade = 1, pesoMedido = 50m, identificadoPorCamera = true });

        // RN3 violada: divergência sem justificativa
        var finalizar = await cliente.PostAsJsonAsync($"{Api}/finalizar", new { justificativa = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, finalizar.StatusCode);
        var gravado = LerDoBanco(api);
        Assert.Equal(StatusProcedimento.EmAndamento, gravado.Status);
        Assert.Null(gravado.DataHoraFim);
        Assert.Equal("", gravado.Observacoes);
        Assert.All(gravado.Itens, i => Assert.Equal(StatusItem.Pendente, i.Status));
    }

    [Fact]
    public async Task ContagemAposFinalizar_ERecusadaEOBancoContinuaComOsValoresFinais()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);
        await cliente.PostAsJsonAsync($"{Api}/finalizar", new { justificativa = (string?)null });

        // RN5 violada: contagem com procedimento Finalizado
        var entrada = await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/entrada", new { quantidade = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, entrada.StatusCode);
        var gravado = LerDoBanco(api);
        Assert.Equal(StatusProcedimento.Finalizado, gravado.Status);
        Assert.Equal(0, gravado.Itens.Single(i => i.Id == itemId).QuantidadeInicial);
    }

    [Fact]
    public async Task Inicializacao_CriaBancoComDadosDeDemonstracao()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();

        var login = await cliente.PostAsJsonAsync("/api/login", new { email = "medico@scanops.com", senha = "123456" });
        var instrumentos = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(2, instrumentos.GetArrayLength());
        Assert.True(File.Exists(api.ArquivoDoBanco));
        var gravado = LerDoBanco(api);
        Assert.Equal(new[] { "Pinça Kelly", "Tesoura Mayo" }, gravado.Itens.Select(i => i.Instrumento.Nome));
    }

    [Fact]
    public async Task ExcluirInstrumentoUsadoEmProcedimento_ERecusadoEInstrumentoPermanece()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var instrumentos = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");
        var idPinca = instrumentos[0].GetProperty("id").GetGuid();

        var exclusao = await cliente.DeleteAsync($"/api/instrumentos/{idPinca}");

        Assert.Equal(HttpStatusCode.BadRequest, exclusao.StatusCode);
        var erro = (await exclusao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.Equal("Instrumento já usado em procedimento não pode ser excluído. Inative-o.", erro);
        var depois = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");
        Assert.Equal(2, depois.GetArrayLength());
    }
}
