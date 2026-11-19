using Microsoft.EntityFrameworkCore;
using ScanOPS.Api;
using ScanOPS.Api.Dtos;
using ScanOPS.Cirurgias;
using ScanOPS.Contagens;
using ScanOPS.Excecoes;
using ScanOPS.Hardware;
using ScanOPS.Instrumentos;
using ScanOPS.Persistencia;
using ScanOPS.Pessoas;
using ScanOPS.Servicos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("front", p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:5174")
     .AllowAnyHeader().AllowAnyMethod()));

// Tratamento central de exceções (ver Api/TratadorDeExcecoes.cs).
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();
builder.Services.AddProblemDetails();

// Persistência: banco SQLite local (appsettings.json -> ConnectionStrings:ScanOps).
builder.Services.AddDbContext<ScanOpsDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("ScanOps")));
builder.Services.AddScoped<IRepositorioDeProcedimentos, RepositorioDeProcedimentos>();
builder.Services.AddScoped<ServicoDeInicioDeProcedimento>(); // RN7: uma sala, um procedimento em andamento

// Hardware atrás de interface: as rotas dependem de IBalanca, não da classe concreta.
// A implementação (BalancaSimulada ou BalancaComRuido) vem de appsettings.json -> "Balanca:Tipo".
builder.Services.AdicionarBalanca(builder.Configuration);
builder.Services.AddSingleton<ServicoDePesagem>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("front");

// Cria o banco e os dados de demonstração na primeira execução.
using (var escopo = app.Services.CreateScope())
    InicializadorDoBanco.Inicializar(escopo.ServiceProvider.GetRequiredService<ScanOpsDbContext>());

// As rotas não capturam exceções: o domínio lança e o TratadorDeExcecoes responde
// (RegraDeNegocioException -> 400, KeyNotFoundException -> 404, FalhaDeHardwareException -> 503).

// ===================== USUÁRIOS (login) =====================

app.MapGet("/api/teste", () => new { mensagem = "API funcionando!" });

app.MapPost("/api/login", (LoginRequisicao dados, ScanOpsDbContext db) =>
{
    // Normaliza uma vez só: busca e autenticação usam o mesmo e-mail (espaços nas pontas não recusam o login).
    var email = dados.Email.Trim().ToLower();
    var encontrado = db.Usuarios
        .Where(u => u.Email.ToLower() == email)
        .AsEnumerable()
        .FirstOrDefault(u => u.Autenticar(email, dados.Senha));

    if (encontrado is null)
        return Results.Unauthorized();

    return Results.Ok(LoginResposta.De(encontrado));
});

// ===================== INSTRUMENTOS (CRUD) =====================
// Entrada e saída por DTOs (Api/Dtos): a API nunca recebe nem devolve a entidade de domínio.

app.MapGet("/api/instrumentos", (ScanOpsDbContext db) =>
    db.Instrumentos.OrderBy(i => i.Nome).AsEnumerable().Select(InstrumentoResposta.De).ToList());

app.MapGet("/api/instrumentos/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id);
    return item is null ? Results.NotFound() : Results.Ok(InstrumentoResposta.De(item));
});

app.MapPost("/api/instrumentos", (InstrumentoRequisicao dados, ScanOpsDbContext db) =>
{
    var novo = dados.CriarInstrumento();
    db.Instrumentos.Add(novo);
    db.SaveChanges();
    return Results.Created($"/api/instrumentos/{novo.Id}", InstrumentoResposta.De(novo));
});

app.MapPut("/api/instrumentos/{id}", (Guid id, InstrumentoRequisicao dados, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id);
    if (item is null) return Results.NotFound();

    dados.AplicarEm(item);
    db.SaveChanges();

    return Results.Ok(InstrumentoResposta.De(item));
});

// Inativar/reativar chamam os métodos do domínio. Inativo não entra em procedimento novo (RN6).
app.MapPost("/api/instrumentos/{id}/inativar", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id) ?? throw new KeyNotFoundException("Instrumento não encontrado.");
    item.Inativar();
    db.SaveChanges();
    return Results.Ok(InstrumentoResposta.De(item));
});

app.MapPost("/api/instrumentos/{id}/reativar", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id) ?? throw new KeyNotFoundException("Instrumento não encontrado.");
    item.Reativar();
    db.SaveChanges();
    return Results.Ok(InstrumentoResposta.De(item));
});

app.MapDelete("/api/instrumentos/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id);
    if (item is null) return Results.NotFound();

    if (db.Set<ItemProcedimento>().Any(i => i.Instrumento.Id == id))
        throw new RegraDeNegocioException("Instrumento já usado em procedimento não pode ser excluído. Inative-o.");

    db.Instrumentos.Remove(item);
    db.SaveChanges();
    return Results.NoContent();
});

// ===================== PACIENTES (CRUD) =====================

app.MapGet("/api/pacientes", (ScanOpsDbContext db) =>
    db.Pacientes.OrderBy(p => p.Nome).AsEnumerable().Select(PacienteResposta.De).ToList());

app.MapGet("/api/pacientes/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Pacientes.Find(id);
    return item is null ? Results.NotFound() : Results.Ok(PacienteResposta.De(item));
});

app.MapPost("/api/pacientes", (PacienteRequisicao dados, ScanOpsDbContext db) =>
{
    var novo = dados.CriarPaciente();
    db.Pacientes.Add(novo);
    db.SaveChanges();
    return Results.Created($"/api/pacientes/{novo.Id}", PacienteResposta.De(novo));
});

app.MapPut("/api/pacientes/{id}", (Guid id, PacienteRequisicao dados, ScanOpsDbContext db) =>
{
    var item = db.Pacientes.Find(id);
    if (item is null) return Results.NotFound();

    dados.AplicarEm(item);
    db.SaveChanges();

    return Results.Ok(PacienteResposta.De(item));
});

app.MapDelete("/api/pacientes/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Pacientes.Find(id);
    if (item is null) return Results.NotFound();

    if (db.Procedimentos.Any(p => p.Paciente.Id == id))
        throw new RegraDeNegocioException("Paciente com procedimento registrado não pode ser excluído: o histórico cirúrgico precisa ser preservado.");

    db.Pacientes.Remove(item);
    db.SaveChanges();
    return Results.NoContent();
});

// ===================== PROCEDIMENTOS (planejar, iniciar, cancelar) =====================

app.MapPost("/api/procedimentos", (NovoProcedimentoRequisicao dados, ScanOpsDbContext db, IRepositorioDeProcedimentos repositorio) =>
{
    var procedimento = new Procedimento(
        dados.Tipo,
        dados.Sala,
        dados.Descricao,
        db.Pacientes.Find(dados.PacienteId) ?? throw new KeyNotFoundException("Paciente não encontrado."),
        db.Usuarios.Find(dados.CirurgiaoId) ?? throw new KeyNotFoundException("Cirurgião não encontrado."),
        db.Usuarios.Find(dados.InstrumentistaId) ?? throw new KeyNotFoundException("Instrumentista não encontrado."));

    foreach (var previsto in dados.Itens ?? [])
    {
        var instrumento = db.Instrumentos.Find(previsto.InstrumentoId)
            ?? throw new KeyNotFoundException("Instrumento não encontrado.");
        procedimento.AdicionarInstrumento(instrumento, previsto.QuantidadePrevista);
    }

    repositorio.Adicionar(procedimento);
    repositorio.Salvar();
    return Results.Created($"/api/procedimentos/{procedimento.Id}", ProcedimentoResposta.De(procedimento));
});

static Procedimento ObterPorId(IRepositorioDeProcedimentos repositorio, Guid id) =>
    repositorio.ObterPorId(id) ?? throw new KeyNotFoundException("Procedimento não encontrado.");

// Histórico: Finalizados e Cancelados, os mais recentes primeiro. Só leitura: não existe
// rota de edição (PUT) nem de exclusão (DELETE) de procedimento.
app.MapGet("/api/procedimentos/historico", (IRepositorioDeProcedimentos repositorio) =>
    repositorio.ListarPorStatus(StatusProcedimento.Finalizado, StatusProcedimento.Cancelado)
        .OrderByDescending(p => p.DataHoraFim)
        .Select(ProcedimentoResumoResposta.De).ToList());

// Planejados ainda não iniciados (para iniciar ou cancelar depois).
app.MapGet("/api/procedimentos/planejados", (IRepositorioDeProcedimentos repositorio) =>
    repositorio.ListarPorStatus(StatusProcedimento.Planejado)
        .OrderBy(p => p.Sala).ThenBy(p => p.Tipo)
        .Select(ProcedimentoResumoResposta.De).ToList());

// Detalhe completo (dados, datas, duração, justificativa/motivo e itens).
app.MapGet("/api/procedimentos/{id:guid}", (Guid id, IRepositorioDeProcedimentos repositorio) =>
    ProcedimentoDetalheResposta.De(ObterPorId(repositorio, id)));

// RN1 (só Planejado) e RN7 (sala livre), pelo serviço de domínio.
app.MapPost("/api/procedimentos/{id:guid}/iniciar", (Guid id, ServicoDeInicioDeProcedimento inicio) =>
    Results.Ok(ProcedimentoResposta.De(inicio.Iniciar(id))));

app.MapPost("/api/procedimentos/{id:guid}/cancelar", (Guid id, CancelarRequisicao dados, IRepositorioDeProcedimentos repositorio) =>
{
    var procedimento = ObterPorId(repositorio, id);
    procedimento.Cancelar(dados.Motivo);
    repositorio.Salvar();
    return Results.Ok(ProcedimentoResposta.De(procedimento));
});

// ===================== SALAS =====================

// Salas ocupadas: o procedimento EmAndamento de cada uma (RN7 garante no máximo um por sala).
app.MapGet("/api/salas/em-uso", (IRepositorioDeProcedimentos repositorio) =>
    repositorio.ListarPorStatus(StatusProcedimento.EmAndamento)
        .OrderBy(p => p.Sala)
        .Select(SalaEmUsoResposta.De).ToList());

// ===================== OPERAÇÃO (contagem e finalização) =====================
// Cada operação: carrega o agregado -> chama o método de domínio (que aplica as regras)
// -> grava. Se o domínio recusar, nada é gravado. As mesmas funções atendem as rotas por id
// (usadas pelo front) e as rotas antigas /api/procedimento-atual (mantidas por compatibilidade).

static IResult Entrada(Procedimento procedimento, Guid itemId, EntradaRequisicao dados, IRepositorioDeProcedimentos repositorio)
{
    procedimento.RegistrarEntrada(itemId, dados.Quantidade);
    repositorio.Salvar();
    return Results.Ok();
}

static IResult Saida(Procedimento procedimento, Guid itemId, SaidaRequisicao dados, IRepositorioDeProcedimentos repositorio)
{
    procedimento.RegistrarSaida(itemId, dados.Quantidade, dados.PesoMedido, dados.IdentificadoPorCamera);
    repositorio.Salvar();
    return Results.Ok();
}

static IResult Pesar(Procedimento procedimento, Guid itemId, ServicoDePesagem pesagem, IRepositorioDeProcedimentos repositorio)
{
    var peso = pesagem.PesarItem(procedimento, itemId);
    repositorio.Salvar();
    return Results.Ok(new PesagemResposta(peso));
}

static IResult Finalizar(Procedimento procedimento, FinalizarRequisicao dados, IRepositorioDeProcedimentos repositorio)
{
    procedimento.Finalizar(dados.Justificativa);
    repositorio.Salvar();
    return Results.Ok(new FinalizacaoResposta(procedimento.Status.ToString()));
}

// Rotas por id: várias salas podem ter procedimentos em andamento ao mesmo tempo.
app.MapPost("/api/procedimentos/{id:guid}/itens/{itemId:guid}/entrada", (Guid id, Guid itemId, EntradaRequisicao dados, IRepositorioDeProcedimentos repositorio) =>
    Entrada(ObterPorId(repositorio, id), itemId, dados, repositorio));
app.MapPost("/api/procedimentos/{id:guid}/itens/{itemId:guid}/saida", (Guid id, Guid itemId, SaidaRequisicao dados, IRepositorioDeProcedimentos repositorio) =>
    Saida(ObterPorId(repositorio, id), itemId, dados, repositorio));
app.MapPost("/api/procedimentos/{id:guid}/itens/{itemId:guid}/pesar", (Guid id, Guid itemId, ServicoDePesagem pesagem, IRepositorioDeProcedimentos repositorio) =>
    Pesar(ObterPorId(repositorio, id), itemId, pesagem, repositorio));
app.MapPost("/api/procedimentos/{id:guid}/finalizar", (Guid id, FinalizarRequisicao dados, IRepositorioDeProcedimentos repositorio) =>
    Finalizar(ObterPorId(repositorio, id), dados, repositorio));

// ===================== PROCEDIMENTO ATUAL (rotas antigas, mantidas por compatibilidade) =====================
// "Atual" = procedimento com início mais recente (qualquer status). Com várias salas isso é
// ambíguo, por isso o front usa as rotas por id; estas continuam para os testes e evidências existentes.

static Procedimento ObterAtual(IRepositorioDeProcedimentos repositorio) =>
    repositorio.ObterAtual() ?? throw new KeyNotFoundException("Nenhum procedimento cadastrado.");

app.MapGet("/api/procedimento-atual", (IRepositorioDeProcedimentos repositorio) =>
    ProcedimentoResposta.De(ObterAtual(repositorio)));
app.MapPost("/api/procedimento-atual/itens/{itemId}/entrada", (Guid itemId, EntradaRequisicao dados, IRepositorioDeProcedimentos repositorio) =>
    Entrada(ObterAtual(repositorio), itemId, dados, repositorio));
app.MapPost("/api/procedimento-atual/itens/{itemId}/saida", (Guid itemId, SaidaRequisicao dados, IRepositorioDeProcedimentos repositorio) =>
    Saida(ObterAtual(repositorio), itemId, dados, repositorio));
app.MapPost("/api/procedimento-atual/itens/{itemId}/pesar", (Guid itemId, ServicoDePesagem pesagem, IRepositorioDeProcedimentos repositorio) =>
    Pesar(ObterAtual(repositorio), itemId, pesagem, repositorio));
app.MapPost("/api/procedimento-atual/finalizar", (FinalizarRequisicao dados, IRepositorioDeProcedimentos repositorio) =>
    Finalizar(ObterAtual(repositorio), dados, repositorio));

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory).
public partial class Program { }
