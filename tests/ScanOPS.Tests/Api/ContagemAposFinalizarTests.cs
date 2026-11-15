using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>
/// Reprodução automatizada do defeito registrado em docs/av2/registro-depuracao.md:
/// a API aceitava contagens em procedimento já Finalizado e quantidades negativas.
/// Cada teste cria uma API nova (estado em memória isolado).
/// </summary>
public class ContagemAposFinalizarTests
{
    private const string Api = "/api/procedimento-atual";

    private static async Task<Guid> PrimeiroItemAsync(HttpClient cliente)
    {
        var json = await cliente.GetFromJsonAsync<JsonElement>(Api);
        return json.GetProperty("itens")[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task RegistrarEntrada_DepoisDeFinalizado_RetornaBadRequestENaoAlteraContagem()
    {
        using var fabrica = new ApiDeTeste();
        var cliente = fabrica.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);

        var finalizar = await cliente.PostAsJsonAsync($"{Api}/finalizar", new { justificativa = (string?)null });
        Assert.Equal(HttpStatusCode.OK, finalizar.StatusCode);

        var entrada = await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/entrada", new { quantidade = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, entrada.StatusCode);
        var depois = await cliente.GetFromJsonAsync<JsonElement>(Api);
        Assert.Equal(0, depois.GetProperty("itens")[0].GetProperty("quantidadeInicial").GetInt32());
    }

    [Fact]
    public async Task RegistrarEntrada_QuantidadeNegativa_RetornaBadRequest()
    {
        using var fabrica = new ApiDeTeste();
        var cliente = fabrica.CreateClient();
        var itemId = await PrimeiroItemAsync(cliente);

        var entrada = await cliente.PostAsJsonAsync($"{Api}/itens/{itemId}/entrada", new { quantidade = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, entrada.StatusCode);
    }
}
