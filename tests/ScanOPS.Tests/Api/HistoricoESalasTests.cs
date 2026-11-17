using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>
/// Histórico de cirurgias (só leitura), salas em uso, planejados e a "foto" do instrumento.
/// O banco de demonstração já tem a Apendicectomia EmAndamento na Sala 1.
/// </summary>
public class HistoricoESalasTests
{
    private sealed record Ids(Guid PacienteId, Guid UsuarioId, Guid PincaId);

    private static async Task<Ids> IdsAsync(HttpClient cliente)
    {
        var login = await (await cliente.PostAsJsonAsync("/api/login", new { email = "medico@scanops.com", senha = "123456" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var pacientes = await cliente.GetFromJsonAsync<JsonElement>("/api/pacientes");
        var instrumentos = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");
        var pinca = instrumentos.EnumerateArray().First(i => i.GetProperty("nome").GetString() == "Pinça Kelly");
        return new Ids(pacientes[0].GetProperty("id").GetGuid(), login.GetProperty("id").GetGuid(), pinca.GetProperty("id").GetGuid());
    }

    private static async Task<Guid> PlanejarAsync(HttpClient cliente, Ids ids, string tipo, string sala, int quantidade = 2)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/procedimentos", new
        {
            tipo, sala, descricao = "Teste de histórico",
            pacienteId = ids.PacienteId, cirurgiaoId = ids.UsuarioId, instrumentistaId = ids.UsuarioId,
            itens = new[] { new { instrumentoId = ids.PincaId, quantidadePrevista = quantidade } }
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> PrimeiroItemAsync(HttpClient cliente, Guid procedimentoId)
    {
        var detalhe = await cliente.GetFromJsonAsync<JsonElement>($"/api/procedimentos/{procedimentoId}");
        return detalhe.GetProperty("itens")[0].GetProperty("id").GetGuid();
    }

    /// <summary>Hernioplastia na Sala 2: entrada 2, saída 1 (falta uma pinça), finalizada com justificativa.</summary>
    private static async Task<Guid> FinalizadoComDivergenciaAsync(HttpClient cliente, Ids ids)
    {
        var id = await PlanejarAsync(cliente, ids, "Hernioplastia", "Sala 2");
        await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);
        var item = await PrimeiroItemAsync(cliente, id);
        await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/itens/{item}/entrada", new { quantidade = 2 });
        await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/itens/{item}/saida", new { quantidade = 1, pesoMedido = 50m, identificadoPorCamera = true });
        var fim = await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/finalizar", new { justificativa = "Pinça localizada na mesa auxiliar." });
        Assert.Equal(HttpStatusCode.OK, fim.StatusCode);
        return id;
    }

    // ---------- histórico ----------

    [Fact]
    public async Task Historico_ListaSoFinalizadosECancelados_MaisRecentePrimeiro()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var finalizado = await FinalizadoComDivergenciaAsync(cliente, ids);
        var cancelado = await PlanejarAsync(cliente, ids, "Biópsia", "Sala 3");
        await cliente.PostAsJsonAsync($"/api/procedimentos/{cancelado}/cancelar", new { motivo = "Paciente transferido." });
        var planejado = await PlanejarAsync(cliente, ids, "Colecistectomia", "Sala 4");

        var historico = await cliente.GetFromJsonAsync<JsonElement>("/api/procedimentos/historico");

        var idsNoHistorico = historico.EnumerateArray().Select(h => h.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(new[] { cancelado, finalizado }, idsNoHistorico); // nem o planejado, nem a demo em andamento
        Assert.DoesNotContain(planejado, idsNoHistorico);
        var linha = historico[1];
        Assert.Equal("Finalizado", linha.GetProperty("status").GetString());
        Assert.Equal("Sala 2", linha.GetProperty("sala").GetString());
        Assert.Equal("Paciente Demo", linha.GetProperty("paciente").GetString());
        Assert.Equal("Dr. Teste", linha.GetProperty("cirurgiao").GetString());
        Assert.True(linha.GetProperty("teveDivergencia").GetBoolean());
    }

    [Fact]
    public async Task Detalhe_FinalizadoTrazTodasAsInformacoesEOsItens()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await FinalizadoComDivergenciaAsync(cliente, await IdsAsync(cliente));

        var d = await cliente.GetFromJsonAsync<JsonElement>($"/api/procedimentos/{id}");

        Assert.Equal("Hernioplastia", d.GetProperty("tipo").GetString());
        Assert.Equal("Teste de histórico", d.GetProperty("descricao").GetString());
        Assert.Equal("Dr. Teste", d.GetProperty("instrumentista").GetString());
        Assert.NotEqual(JsonValueKind.Null, d.GetProperty("dataHoraInicio").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, d.GetProperty("dataHoraFim").ValueKind);
        Assert.True(d.GetProperty("duracaoMinutos").GetInt32() >= 0);
        Assert.Equal("Pinça localizada na mesa auxiliar.", d.GetProperty("justificativa").GetString());
        var item = d.GetProperty("itens")[0];
        Assert.Equal("Pinça Kelly", item.GetProperty("instrumento").GetString());
        Assert.Equal(2, item.GetProperty("quantidadePrevista").GetInt32());
        Assert.Equal(2, item.GetProperty("quantidadeInicial").GetInt32());
        Assert.Equal(1, item.GetProperty("quantidadeFinal").GetInt32());
        Assert.Equal(50m, item.GetProperty("pesoMedido").GetDecimal());
        Assert.Equal(100m, item.GetProperty("pesoEsperado").GetDecimal());
        Assert.Equal("Divergente", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Detalhe_CanceladoTrazMotivoEHoraDoCancelamento()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await PlanejarAsync(cliente, await IdsAsync(cliente), "Biópsia", "Sala 3");
        await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/cancelar", new { motivo = "Paciente transferido." });

        var d = await cliente.GetFromJsonAsync<JsonElement>($"/api/procedimentos/{id}");

        Assert.Equal("Cancelado", d.GetProperty("status").GetString());
        Assert.Equal("Paciente transferido.", d.GetProperty("motivoCancelamento").GetString());
        Assert.Equal(JsonValueKind.Null, d.GetProperty("dataHoraInicio").ValueKind); // nunca começou
        Assert.NotEqual(JsonValueKind.Null, d.GetProperty("dataHoraFim").ValueKind);
        Assert.Equal(JsonValueKind.Null, d.GetProperty("duracaoMinutos").ValueKind);
    }

    // ---------- histórico não pode ser apagado nem alterado ----------

    [Fact]
    public async Task Procedimento_NaoTemRotaDeEdicaoNemDeExclusao()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await FinalizadoComDivergenciaAsync(cliente, await IdsAsync(cliente));

        var put = await cliente.PutAsJsonAsync($"/api/procedimentos/{id}", new { tipo = "Outro" });
        var delete = await cliente.DeleteAsync($"/api/procedimentos/{id}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, put.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);
        var d = await cliente.GetFromJsonAsync<JsonElement>($"/api/procedimentos/{id}");
        Assert.Equal("Hernioplastia", d.GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task ProcedimentoFinalizado_RecusaTodaOperacaoPelaApi()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var id = await FinalizadoComDivergenciaAsync(cliente, await IdsAsync(cliente));
        var item = await PrimeiroItemAsync(cliente, id);

        var tentativas = new[]
        {
            await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/itens/{item}/entrada", new { quantidade = 1 }),
            await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/itens/{item}/saida", new { quantidade = 1, pesoMedido = 50m, identificadoPorCamera = true }),
            await cliente.PostAsync($"/api/procedimentos/{id}/itens/{item}/pesar", null),
            await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/finalizar", new { justificativa = "de novo" }),
            await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null),
            await cliente.PostAsJsonAsync($"/api/procedimentos/{id}/cancelar", new { motivo = "tentativa" }),
        };

        Assert.All(tentativas, r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        var d = await cliente.GetFromJsonAsync<JsonElement>($"/api/procedimentos/{id}");
        Assert.Equal("Finalizado", d.GetProperty("status").GetString());
        Assert.Equal(1, d.GetProperty("itens")[0].GetProperty("quantidadeFinal").GetInt32());
        Assert.Equal(50m, d.GetProperty("itens")[0].GetProperty("pesoMedido").GetDecimal());
    }

    [Fact]
    public async Task ExcluirPacienteUsadoEmProcedimento_Retorna400EPacientePermanece()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente); // Paciente Demo está na Apendicectomia

        var resposta = await cliente.DeleteAsync($"/api/pacientes/{ids.PacienteId}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.Contains("histórico cirúrgico precisa ser preservado", erro);
        var paciente = await cliente.GetAsync($"/api/pacientes/{ids.PacienteId}");
        Assert.Equal(HttpStatusCode.OK, paciente.StatusCode);
    }

    // ---------- "foto" do instrumento ----------

    [Fact]
    public async Task Foto_InstrumentoRenomeadoEComPesoCorrigidoDepois_NaoMudaOItem()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var id = await PlanejarAsync(cliente, ids, "Hernioplastia", "Sala 2");

        await cliente.PutAsJsonAsync($"/api/instrumentos/{ids.PincaId}", new
        {
            nome = "Pinça Kelly Curva", codigo = "P001", tipo = "Pinca", material = "", tamanho = "", pesoReferencia = 55
        });

        var item = (await cliente.GetFromJsonAsync<JsonElement>($"/api/procedimentos/{id}")).GetProperty("itens")[0];
        Assert.Equal("Pinça Kelly", item.GetProperty("instrumento").GetString());
        Assert.Equal(50m, item.GetProperty("pesoReferencia").GetDecimal());
        var cadastro = await cliente.GetFromJsonAsync<JsonElement>($"/api/instrumentos/{ids.PincaId}");
        Assert.Equal("Pinça Kelly Curva", cadastro.GetProperty("nome").GetString()); // o cadastro mudou
    }

    // ---------- salas em uso e planejados ----------

    [Fact]
    public async Task SalasEmUso_MostraCadaSalaOcupadaComSeuProcedimento()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var id = await PlanejarAsync(cliente, ids, "Hernioplastia", "2");
        await cliente.PostAsync($"/api/procedimentos/{id}/iniciar", null);

        var salas = await cliente.GetFromJsonAsync<JsonElement>("/api/salas/em-uso");

        Assert.Equal(2, salas.GetArrayLength());
        Assert.Equal("Sala 1", salas[0].GetProperty("sala").GetString());
        Assert.Equal("Apendicectomia", salas[0].GetProperty("tipo").GetString());
        Assert.Equal("Sala 2", salas[1].GetProperty("sala").GetString());
        Assert.Equal(id, salas[1].GetProperty("procedimentoId").GetGuid());
        Assert.Equal("Paciente Demo", salas[1].GetProperty("paciente").GetString());
        Assert.Equal("Dr. Teste", salas[1].GetProperty("cirurgiao").GetString());
        Assert.NotEqual(JsonValueKind.Null, salas[1].GetProperty("dataHoraInicio").ValueKind);
    }

    [Fact]
    public async Task Planejados_ListaSoOsQueAindaNaoComecaram()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var ids = await IdsAsync(cliente);
        var a = await PlanejarAsync(cliente, ids, "Hernioplastia", "Sala 2");
        var b = await PlanejarAsync(cliente, ids, "Colecistectomia", "Sala 3");
        await cliente.PostAsync($"/api/procedimentos/{a}/iniciar", null);

        var planejados = await cliente.GetFromJsonAsync<JsonElement>("/api/procedimentos/planejados");

        Assert.Equal(new[] { b }, planejados.EnumerateArray().Select(p => p.GetProperty("id").GetGuid()));
    }
}
