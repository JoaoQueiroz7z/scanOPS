using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ScanOPS.Instrumentos;
using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>Inativar e reativar instrumento (métodos do domínio expostos pela API).</summary>
public class InstrumentoAtivoTests
{
    [Fact]
    public void Dominio_InativarEReativar_MudamOAtivo()
    {
        var instrumento = new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m };
        Assert.True(instrumento.Ativo);

        instrumento.Inativar();
        Assert.False(instrumento.Ativo);

        instrumento.Reativar();
        Assert.True(instrumento.Ativo);
    }

    [Fact]
    public void Dominio_AtivoNaoTemSetterPublico()
    {
        Assert.Null(typeof(Instrumento).GetProperty(nameof(Instrumento.Ativo))!.GetSetMethod(nonPublic: false));
    }

    private static async Task<Guid> PrimeiroInstrumentoAsync(HttpClient cliente)
    {
        var lista = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");
        return lista[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Api_Inativar_GravaAtivoFalso()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PrimeiroInstrumentoAsync(cliente);

        var resposta = await cliente.PostAsync($"/api/instrumentos/{id}/inativar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var gravado = await cliente.GetFromJsonAsync<JsonElement>($"/api/instrumentos/{id}");
        Assert.False(gravado.GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task Api_ReativarDepoisDeInativar_GravaAtivoVerdadeiro()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PrimeiroInstrumentoAsync(cliente);
        await cliente.PostAsync($"/api/instrumentos/{id}/inativar", null);

        var resposta = await cliente.PostAsync($"/api/instrumentos/{id}/reativar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var gravado = await cliente.GetFromJsonAsync<JsonElement>($"/api/instrumentos/{id}");
        Assert.True(gravado.GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task Api_InativarInstrumentoInexistente_Retorna404()
    {
        using var api = new ApiDeTeste();

        var resposta = await api.CreateClient().PostAsync($"/api/instrumentos/{Guid.NewGuid()}/inativar", null);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
