using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ScanOPS.Api;
using ScanOPS.Tests.Infra;

namespace ScanOPS.Tests.Api;

/// <summary>
/// Contrato da API (DTOs em Api/Dtos): o que o front envia e o que recebe.
/// Os pedidos abaixo reproduzem exatamente o JSON que as telas do frontend montam.
/// </summary>
public class ContratoApiTests
{
    // ---------- Instrumentos ----------

    [Fact]
    public async Task CriarInstrumento_ComoATelaEnvia_TipoEmTextoEPesoEmTexto_Retorna201()
    {
        // Antes dos DTOs: 400, porque a API esperava o tipo como número (defeito encontrado no passo 7).
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/api/instrumentos", new
        {
            nome = "Afastador Farabeuf", codigo = "A001", tipo = "Afastador",
            material = "Inox", tamanho = "15cm", pesoReferencia = "35"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Afastador", criado.GetProperty("tipo").GetString());
        Assert.Equal(35m, criado.GetProperty("pesoReferencia").GetDecimal());
    }

    [Fact]
    public async Task ListarInstrumentos_DevolveTipoComoTexto()
    {
        // Antes: "tipo": 1, e a tela mostrava "1" em vez do nome do tipo.
        using var api = new ApiDeTeste();
        var lista = await api.CreateClient().GetFromJsonAsync<JsonElement>("/api/instrumentos");

        Assert.Equal("Pinca", lista[0].GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task CriarInstrumento_TipoInexistente_Retorna400ListandoOsTiposValidos()
    {
        using var api = new ApiDeTeste();

        var resposta = await api.CreateClient().PostAsJsonAsync("/api/instrumentos", new
        {
            nome = "X", codigo = "X1", tipo = "Martelo", material = "", tamanho = "", pesoReferencia = 10
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.Contains("Martelo", erro);
        Assert.Contains("Pinca", erro);
    }

    [Fact]
    public async Task CriarInstrumento_IgnoraIdEAtivoEnviadosPeloCliente()
    {
        // A requisição não tem Id nem Ativo: o cliente não escolhe a identidade nem cria já inativo.
        using var api = new ApiDeTeste();
        var idEscolhidoPeloCliente = Guid.NewGuid();

        var resposta = await api.CreateClient().PostAsJsonAsync("/api/instrumentos", new
        {
            id = idEscolhidoPeloCliente, ativo = false,
            nome = "Pinça Allis", codigo = "P002", tipo = "Pinca", material = "", tamanho = "", pesoReferencia = 40
        });

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(idEscolhidoPeloCliente, criado.GetProperty("id").GetGuid());
        Assert.True(criado.GetProperty("ativo").GetBoolean());
    }

    // ---------- Pacientes ----------

    [Fact]
    public async Task CriarPaciente_SemDataDeNascimento_Retorna201ComDataEIdadeNulas()
    {
        // Antes: 400, porque o formulário envia "" e a API não aceitava data vazia.
        using var api = new ApiDeTeste();

        var resposta = await api.CreateClient().PostAsJsonAsync("/api/pacientes", new
        {
            nome = "Maria", dataNascimento = "", sexo = "F", tipoSanguineo = "O+", alergias = "", observacoes = ""
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, criado.GetProperty("dataNascimento").ValueKind);
        Assert.Equal(JsonValueKind.Null, criado.GetProperty("idade").ValueKind);
    }

    [Fact]
    public async Task CriarPaciente_ComDataDeNascimento_DevolveIdadeCalculadaPeloDominio()
    {
        using var api = new ApiDeTeste();
        var nascimento = DateOnly.FromDateTime(DateTime.Today).AddYears(-30);

        var resposta = await api.CreateClient().PostAsJsonAsync("/api/pacientes", new
        {
            nome = "João", dataNascimento = nascimento.ToString("yyyy-MM-dd"), sexo = "M", tipoSanguineo = "A+", alergias = "", observacoes = ""
        });

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nascimento.ToString("yyyy-MM-dd"), criado.GetProperty("dataNascimento").GetString());
        Assert.Equal(30, criado.GetProperty("idade").GetInt32());
    }

    [Fact]
    public async Task CriarPaciente_DataEmFormatoInvalido_Retorna400ComMensagem()
    {
        using var api = new ApiDeTeste();

        var resposta = await api.CreateClient().PostAsJsonAsync("/api/pacientes", new
        {
            nome = "Ana", dataNascimento = "31/02/1990", sexo = "F", tipoSanguineo = "", alergias = "", observacoes = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.Contains("Data de nascimento inválida", erro);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_DevolveCargoComoTextoESemDadosDeSenha()
    {
        using var api = new ApiDeTeste();

        var resposta = await api.CreateClient().PostAsJsonAsync("/api/login", new { email = "medico@scanops.com", senha = "123456" });

        var texto = await resposta.Content.ReadAsStringAsync();
        var corpo = JsonDocument.Parse(texto).RootElement;
        Assert.Equal("Cirurgiao", corpo.GetProperty("cargo").GetString());
        Assert.DoesNotContain("senha", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", texto, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- JSON fora do formato ----------

    [Fact]
    public async Task JsonComTipoErrado_Retorna400ComMensagemGenericaENao500()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var procedimento = await cliente.GetFromJsonAsync<JsonElement>("/api/procedimento-atual");
        var itemId = procedimento.GetProperty("itens")[0].GetProperty("id").GetGuid();

        var resposta = await cliente.PostAsync($"/api/procedimento-atual/itens/{itemId}/entrada",
            new StringContent("{\"quantidade\":\"muitas\"}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.Equal(TratadorDeExcecoes.MensagemRequisicaoInvalida, erro);
    }

    // ---------- Procedimento: dados para a tela ----------

    [Fact]
    public async Task ProcedimentoAtual_DevolveSalaENomesDoPacienteEDoCirurgiao()
    {
        using var api = new ApiDeTeste();

        var atual = await api.CreateClient().GetFromJsonAsync<JsonElement>("/api/procedimento-atual");

        // Dados de demonstração gravados por Persistencia/InicializadorDoBanco.cs
        Assert.Equal("Sala 1", atual.GetProperty("sala").GetString());
        Assert.Equal("Paciente Demo", atual.GetProperty("paciente").GetString());
        Assert.Equal("Dr. Teste", atual.GetProperty("cirurgiao").GetString());
    }

    [Fact]
    public async Task PlanejarProcedimento_RespostaTrazSalaPacienteECirurgiaoInformados()
    {
        using var api = new ApiDeTeste();
        var cliente = api.CreateClient();
        var usuario = await (await cliente.PostAsJsonAsync("/api/login", new { email = "medico@scanops.com", senha = "123456" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var paciente = await (await cliente.PostAsJsonAsync("/api/pacientes", new
        {
            nome = "Ana Souza", dataNascimento = "", sexo = "F", tipoSanguineo = "", alergias = "", observacoes = ""
        })).Content.ReadFromJsonAsync<JsonElement>();
        var instrumentos = await cliente.GetFromJsonAsync<JsonElement>("/api/instrumentos");
        var idUsuario = usuario.GetProperty("id").GetGuid();

        var resposta = await cliente.PostAsJsonAsync("/api/procedimentos", new
        {
            tipo = "Colecistectomia", sala = "Sala 3", descricao = (string?)null,
            pacienteId = paciente.GetProperty("id").GetGuid(), cirurgiaoId = idUsuario, instrumentistaId = idUsuario,
            itens = new[] { new { instrumentoId = instrumentos[0].GetProperty("id").GetGuid(), quantidadePrevista = 2 } }
        });

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("Sala 3", criado.GetProperty("sala").GetString());
        Assert.Equal("Ana Souza", criado.GetProperty("paciente").GetString());
        Assert.Equal("Dr. Teste", criado.GetProperty("cirurgiao").GetString());
        Assert.Equal("Planejado", criado.GetProperty("status").GetString());
    }
}
