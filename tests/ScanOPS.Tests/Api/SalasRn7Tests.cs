using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScanOPS.Cirurgias;
using ScanOPS.Excecoes;
using ScanOPS.Persistencia;
using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>
/// RN7: duas cirurgias não podem acontecer na mesma sala ao mesmo tempo.
/// Testes de integração pela API (WebApplicationFactory) conferindo o que ficou gravado no banco.
/// O procedimento de demonstração (Apendicectomia) já está EmAndamento na Sala 1.
/// </summary>
public class SalasRn7Tests
{
    private sealed record Ids(Guid PacienteId, Guid UsuarioId, Guid InstrumentoId);

    private static async Task<Ids> IdsAsync(HttpClient cliente)
    {
        var login = await (await cliente.PostAsJsonAsync("/api/login", new { email = "medico@scanops.com", senha = "123456" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var pacientes = await cliente.GetFromJsonAsync<JsonElement>("/api/pacientes");
        var instrumentos = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");
        return new Ids(pacientes[0].GetProperty("id").GetGuid(), login.GetProperty("id").GetGuid(),
            instrumentos[0].GetProperty("id").GetGuid());
    }

    private static async Task<Guid> PlanejarAsync(HttpClient cliente, Ids ids, string tipo, string sala)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/procedimentos", new
        {
            tipo, sala, descricao = (string?)null,
            pacienteId = ids.PacienteId, cirurgiaoId = ids.UsuarioId, instrumentistaId = ids.UsuarioId,
            itens = new[] { new { instrumentoId = ids.InstrumentoId, quantidadePrevista = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static Procedimento LerDoBanco(ApiDeTeste api, Guid id)
    {
        using var escopo = api.Services.CreateScope();
        return escopo.ServiceProvider.GetRequiredService<ScanOpsDbContext>().Procedimentos.AsNoTracking().Single(p => p.Id == id);
    }

    [Fact]
    public async Task RN7_SalaLivre_IniciaEGravaEmAndamento()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PlanejarAsync(cliente, await IdsAsync(cliente), "Hernioplastia", "Sala 2");

        var resposta = await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(StatusProcedimento.EmAndamento, LerDoBanco(api, id).Status);
    }

    [Fact]
    public async Task RN7_SalaOcupada_Recusa400ComMensagemENadaMudaNoBanco()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var primeiro = await PlanejarAsync(cliente, ids, "Hernioplastia", "Sala 2");
        var segundo = await PlanejarAsync(cliente, ids, "Colecistectomia", "Sala 2"); // planejar na mesma sala é permitido
        await cliente.PostAsync($"/api/procedimentos/{primeiro}/iniciar", null);

        var resposta = await cliente.PostAsync($"/api/procedimentos/{segundo}/iniciar", null);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.StartsWith("A Sala 2 já está ocupada pelo procedimento Hernioplastia.", erro);
        var gravado = LerDoBanco(api, segundo);
        Assert.Equal(StatusProcedimento.Planejado, gravado.Status);
        Assert.Null(gravado.DataHoraInicio);
        Assert.Equal(StatusProcedimento.EmAndamento, LerDoBanco(api, primeiro).Status);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("sala 1")]
    [InlineData("  SALA   01 ")]
    public async Task RN7_SalaEscritaDeOutroJeito_EhAMesmaSala(string salaDigitada)
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PlanejarAsync(cliente, await IdsAsync(cliente), "Biópsia", salaDigitada);

        var resposta = await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);

        Assert.Equal("Sala 1", LerDoBanco(api, id).Sala); // gravada padronizada
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode); // a Sala 1 está com a Apendicectomia
        var erro = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.StartsWith("A Sala 1 já está ocupada pelo procedimento Apendicectomia.", erro);
    }

    [Fact]
    public async Task RN7_DepoisDeFinalizar_SalaFicaLivreDeNovo()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var primeiro = await PlanejarAsync(cliente, ids, "Hernioplastia", "Sala 2");
        var segundo = await PlanejarAsync(cliente, ids, "Colecistectomia", "Sala 2");
        await cliente.PostAsync($"/api/procedimentos/{primeiro}/iniciar", null);
        // nada foi contado: sem divergência, finaliza sem justificativa
        var fim = await cliente.PostAsJsonAsync($"/api/procedimentos/{primeiro}/finalizar", new { justificativa = (string?)null });
        Assert.Equal(HttpStatusCode.OK, fim.StatusCode);

        var resposta = await cliente.PostAsync($"/api/procedimentos/{segundo}/iniciar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(StatusProcedimento.EmAndamento, LerDoBanco(api, segundo).Status);
    }

    [Fact]
    public async Task RN7_ProcedimentoCanceladoNaoOcupaASala()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var cancelado = await PlanejarAsync(cliente, ids, "Hernioplastia", "Sala 3");
        await cliente.PostAsJsonAsync($"/api/procedimentos/{cancelado}/cancelar", new { motivo = "Paciente transferido." });
        var outro = await PlanejarAsync(cliente, ids, "Colecistectomia", "Sala 3");

        var resposta = await cliente.PostAsync($"/api/procedimentos/{outro}/iniciar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task RN7_IndiceDoBanco_BarraDoisEmAndamentoNaMesmaSalaComoRegraENaoErro500()
    {
        // Simula a "corrida": dois inícios na mesma sala sem passar pelo serviço de domínio.
        // Quem barra é o índice único parcial, e o repositório transforma o erro em regra de negócio.
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var a = await PlanejarAsync(cliente, ids, "Hernioplastia", "Sala 4");
        var b = await PlanejarAsync(cliente, ids, "Colecistectomia", "Sala 4");

        using var escopo = api.Services.CreateScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IRepositorioDeProcedimentos>();
        repositorio.ObterPorId(a)!.Iniciar();
        repositorio.ObterPorId(b)!.Iniciar();

        var erro = Assert.Throws<RegraDeNegocioException>(() => repositorio.Salvar());
        Assert.Contains("ocupada", erro.Message);
        Assert.Equal(StatusProcedimento.Planejado, LerDoBanco(api, a).Status);
        Assert.Equal(StatusProcedimento.Planejado, LerDoBanco(api, b).Status);
    }
}
