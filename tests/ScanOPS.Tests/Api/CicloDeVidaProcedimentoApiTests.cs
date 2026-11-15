using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScanOPS.Cirurgias;
using ScanOPS.Persistencia;
using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>
/// Fluxo F1 pela API: planejar um procedimento, iniciá-lo (RN1) ou cancelá-lo,
/// verificando o resultado gravado no banco.
/// </summary>
public class CicloDeVidaProcedimentoApiTests
{
    private sealed record Ids(Guid PacienteId, Guid UsuarioId, Guid PincaId, Guid TesouraId);

    private static async Task<Ids> IdsDaDemonstracaoAsync(HttpClient cliente)
    {
        var login = await (await cliente.PostAsJsonAsync("/api/login", new { email = "medico@scanops.com", senha = "123456" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var pacientes = await cliente.GetFromJsonAsync<JsonElement>("/api/pacientes");
        var instrumentos = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");
        Guid IdDoInstrumento(string nome) => instrumentos.EnumerateArray()
            .First(i => i.GetProperty("nome").GetString() == nome).GetProperty("id").GetGuid();

        return new Ids(
            pacientes[0].GetProperty("id").GetGuid(),
            login.GetProperty("id").GetGuid(),
            IdDoInstrumento("Pinça Kelly"),
            IdDoInstrumento("Tesoura Mayo"));
    }

    private static async Task<Guid> PlanejarAsync(HttpClient cliente, Ids ids)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/procedimentos", new
        {
            tipo = "Colecistectomia", sala = "Sala 2", descricao = (string?)null,
            pacienteId = ids.PacienteId, cirurgiaoId = ids.UsuarioId, instrumentistaId = ids.UsuarioId,
            itens = new[]
            {
                new { instrumentoId = ids.PincaId, quantidadePrevista = 3 },
                new { instrumentoId = ids.TesouraId, quantidadePrevista = 1 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static Procedimento LerDoBanco(ApiDeTeste api, Guid id)
    {
        using var escopo = api.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<ScanOpsDbContext>();
        return db.Procedimentos.AsNoTracking().Include(p => p.Itens).Single(p => p.Id == id);
    }

    [Fact]
    public async Task Planejar_CriaProcedimentoPlanejadoComOsItensPrevistos()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();

        var id = await PlanejarAsync(cliente, await IdsDaDemonstracaoAsync(cliente));

        var gravado = LerDoBanco(api, id);
        Assert.Equal(StatusProcedimento.Planejado, gravado.Status);
        Assert.Equal(new[] { 3, 1 }, gravado.Itens.OrderBy(i => i.Sequencia).Select(i => i.QuantidadePrevista));
    }

    [Fact]
    public async Task RN1_Iniciar_ProcedimentoPlanejado_FicaEmAndamentoGravadoEViraOProcedimentoAtual()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PlanejarAsync(cliente, await IdsDaDemonstracaoAsync(cliente));

        var resposta = await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var gravado = LerDoBanco(api, id);
        Assert.Equal(StatusProcedimento.EmAndamento, gravado.Status);
        Assert.NotNull(gravado.DataHoraInicio);
        var atual = await cliente.GetFromJsonAsync<JsonElement>("/api/procedimento-atual");
        Assert.Equal(id, atual.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task RN1_IniciarDuasVezes_Retorna400EMantemADataDeInicioGravada()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PlanejarAsync(cliente, await IdsDaDemonstracaoAsync(cliente));
        await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);
        var inicioGravado = LerDoBanco(api, id).DataHoraInicio;

        var segunda = await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);

        Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
        var erro = (await segunda.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.Contains("planejado", erro);
        Assert.Equal(inicioGravado, LerDoBanco(api, id).DataHoraInicio);
    }

    [Fact]
    public async Task Cancelar_ProcedimentoPlanejado_FicaCanceladoComMotivoGravado()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PlanejarAsync(cliente, await IdsDaDemonstracaoAsync(cliente));

        var resposta = await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/cancelar", new { motivo = "Paciente transferido." });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var gravado = LerDoBanco(api, id);
        Assert.Equal(StatusProcedimento.Cancelado, gravado.Status);
        Assert.Contains("[Cancelamento] Paciente transferido.", gravado.Observacoes);
    }

    [Fact]
    public async Task Cancelar_ProcedimentoEmAndamento_Retorna400ENaoAlteraStatusGravado()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PlanejarAsync(cliente, await IdsDaDemonstracaoAsync(cliente));
        await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);

        var resposta = await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/cancelar", new { motivo = "Desistência." });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(StatusProcedimento.EmAndamento, LerDoBanco(api, id).Status);
    }

    [Fact]
    public async Task Planejar_QuantidadePrevistaZero_Retorna400ENadaEGravado()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsDaDemonstracaoAsync(cliente);

        var resposta = await cliente.PostAsJsonAsync("/api/procedimentos", new
        {
            tipo = "Teste", sala = "Sala 3", descricao = (string?)null,
            pacienteId = ids.PacienteId, cirurgiaoId = ids.UsuarioId, instrumentistaId = ids.UsuarioId,
            itens = new[] { new { instrumentoId = ids.PincaId, quantidadePrevista = 0 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var escopo = api.Services.CreateScope();
        Assert.Equal(1, escopo.ServiceProvider.GetRequiredService<ScanOpsDbContext>().Procedimentos.Count()); // só o da demonstração
    }

    [Fact]
    public async Task Planejar_PacienteInexistente_Retorna404()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsDaDemonstracaoAsync(cliente);

        var resposta = await cliente.PostAsJsonAsync("/api/procedimentos", new
        {
            tipo = "Teste", sala = "Sala 3", descricao = (string?)null,
            pacienteId = Guid.NewGuid(), cirurgiaoId = ids.UsuarioId, instrumentistaId = ids.UsuarioId,
            itens = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
