using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>
/// Login (/api/login): o e-mail é normalizado (trim + minúsculas) antes de autenticar,
/// então variações acidentais — espaços nas pontas ou maiúsculas — também entram.
/// A senha continua sendo conferida pelo domínio (Usuario.Autenticar).
/// </summary>
public class LoginTests
{
    // Dados de demonstração criados por Persistencia/InicializadorDoBanco.cs.
    private const string EmailDemo = "medico@scanops.com";
    private const string SenhaDemo = "123456";

    private static Task<HttpResponseMessage> EntrarAsync(HttpClient cliente, string email, string senha) =>
        cliente.PostAsJsonAsync("/api/login", new { email, senha });

    [Fact]
    public async Task Login_EmailNormal_Autentica()
    {
        using var api = new ApiDeTeste();

        var resposta = await EntrarAsync(api.CreateClient(), EmailDemo, SenhaDemo);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_EmailComEspacosAntesEDepois_Autentica()
    {
        // Antes da correção: 401 — a busca no banco normalizava o e-mail, mas a
        // autenticação usava o valor original (com espaços) e recusava.
        using var api = new ApiDeTeste();

        var resposta = await EntrarAsync(api.CreateClient(), $"  {EmailDemo}  ", SenhaDemo);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Dr. Teste", corpo.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Login_EmailComLetrasMaiusculasEMinusculas_Autentica()
    {
        using var api = new ApiDeTeste();

        var resposta = await EntrarAsync(api.CreateClient(), "MEDICO@ScanOps.COM", SenhaDemo);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_SenhaIncorreta_Retorna401()
    {
        using var api = new ApiDeTeste();

        var resposta = await EntrarAsync(api.CreateClient(), EmailDemo, "senha-errada");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
