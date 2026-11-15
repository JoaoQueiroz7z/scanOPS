using Microsoft.EntityFrameworkCore;
using ScanOPS.Api;
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

app.MapPost("/api/login", (LoginRequest dados, ScanOpsDbContext db) =>
{
    var email = dados.Email.Trim().ToLower();
    var encontrado = db.Usuarios
        .Where(u => u.Email.ToLower() == email)
        .AsEnumerable()
        .FirstOrDefault(u => u.Autenticar(dados.Email, dados.Senha));

    if (encontrado is null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        encontrado.Id,
        encontrado.Nome,
        encontrado.Cargo
    });
});

// ===================== INSTRUMENTOS (CRUD) =====================

app.MapGet("/api/instrumentos", (ScanOpsDbContext db) => db.Instrumentos.OrderBy(i => i.Nome).ToList());

app.MapGet("/api/instrumentos/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapPost("/api/instrumentos", (Instrumento novo, ScanOpsDbContext db) =>
{
    db.Instrumentos.Add(novo);
    db.SaveChanges();
    return Results.Created($"/api/instrumentos/{novo.Id}", novo);
});

app.MapPut("/api/instrumentos/{id}", (Guid id, Instrumento dados, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id);
    if (item is null) return Results.NotFound();

    item.Editar(dados.Nome, dados.Material, dados.Tamanho);
    item.Codigo = dados.Codigo;
    item.Tipo = dados.Tipo;
    item.PesoReferencia = dados.PesoReferencia;
    db.SaveChanges();

    return Results.Ok(item);
});

app.MapDelete("/api/instrumentos/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Instrumentos.Find(id);
    if (item is null) return Results.NotFound();

    if (db.Set<ItemProcedimento>().Any(i => i.Instrumento.Id == id))
        throw new RegraDeNegocioException("Instrumento já usado em procedimento não pode ser excluído; inative-o.");

    db.Instrumentos.Remove(item);
    db.SaveChanges();
    return Results.NoContent();
});

// ===================== PACIENTES (CRUD) =====================

app.MapGet("/api/pacientes", (ScanOpsDbContext db) => db.Pacientes.OrderBy(p => p.Nome).ToList());

app.MapGet("/api/pacientes/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Pacientes.Find(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapPost("/api/pacientes", (Paciente novo, ScanOpsDbContext db) =>
{
    db.Pacientes.Add(novo);
    db.SaveChanges();
    return Results.Created($"/api/pacientes/{novo.Id}", novo);
});

app.MapPut("/api/pacientes/{id}", (Guid id, Paciente dados, ScanOpsDbContext db) =>
{
    var item = db.Pacientes.Find(id);
    if (item is null) return Results.NotFound();

    item.EditarDados(dados.Nome, dados.Alergias, dados.Observacoes);
    item.DataNascimento = dados.DataNascimento;
    item.Sexo = dados.Sexo;
    item.TipoSanguineo = dados.TipoSanguineo;
    db.SaveChanges();

    return Results.Ok(item);
});

app.MapDelete("/api/pacientes/{id}", (Guid id, ScanOpsDbContext db) =>
{
    var item = db.Pacientes.Find(id);
    if (item is null) return Results.NotFound();

    if (db.Procedimentos.Any(p => p.Paciente.Id == id))
        throw new RegraDeNegocioException("Paciente com procedimento registrado não pode ser excluído.");

    db.Pacientes.Remove(item);
    db.SaveChanges();
    return Results.NoContent();
});

// ===================== PROCEDIMENTO ATUAL =====================
// Cada rota: carrega o agregado pelo repositório -> chama o método de domínio
// (que aplica as regras RN1..RN5) -> grava. Se o domínio recusar, nada é gravado.

static Procedimento ObterAtual(IRepositorioDeProcedimentos repositorio) =>
    repositorio.ObterAtual() ?? throw new KeyNotFoundException("Nenhum procedimento cadastrado.");

app.MapGet("/api/procedimento-atual", (IRepositorioDeProcedimentos repositorio) =>
{
    var procedimento = ObterAtual(repositorio);
    return new
    {
        procedimento.Id,
        procedimento.Tipo,
        Status = procedimento.Status.ToString(),
        Itens = procedimento.Itens.Select(i => new
        {
            i.Id,
            Instrumento = i.Instrumento.Nome,
            i.QuantidadePrevista,
            i.QuantidadeInicial,
            i.QuantidadeFinal,
            i.Diferenca,
            i.PesoMedido,
            i.IdentificadoPorCamera,
            Status = i.Status.ToString()
        })
    };
});

app.MapPost("/api/procedimento-atual/itens/{itemId}/entrada", (Guid itemId, EntradaRequest dados, IRepositorioDeProcedimentos repositorio) =>
{
    var procedimento = ObterAtual(repositorio);
    procedimento.RegistrarEntrada(itemId, dados.Quantidade);
    repositorio.Salvar();
    return Results.Ok();
});

app.MapPost("/api/procedimento-atual/itens/{itemId}/saida", (Guid itemId, SaidaRequest dados, IRepositorioDeProcedimentos repositorio) =>
{
    var procedimento = ObterAtual(repositorio);
    procedimento.RegistrarSaida(itemId, dados.Quantidade, dados.PesoMedido, dados.IdentificadoPorCamera);
    repositorio.Salvar();
    return Results.Ok();
});

app.MapPost("/api/procedimento-atual/itens/{itemId}/pesar", (Guid itemId, ServicoDePesagem pesagem, IRepositorioDeProcedimentos repositorio) =>
{
    var procedimento = ObterAtual(repositorio);
    var peso = pesagem.PesarItem(procedimento, itemId);
    repositorio.Salvar();
    return Results.Ok(new { pesoMedido = peso });
});

app.MapPost("/api/procedimento-atual/finalizar", (FinalizarRequest dados, IRepositorioDeProcedimentos repositorio) =>
{
    var procedimento = ObterAtual(repositorio);
    procedimento.Finalizar(dados.Justificativa);
    repositorio.Salvar();
    return Results.Ok(new { status = procedimento.Status.ToString() });
});

app.Run();

// ===================== REGISTROS AUXILIARES =====================

record LoginRequest(string Email, string Senha);
record EntradaRequest(int Quantidade);
record SaidaRequest(int Quantidade, decimal PesoMedido, bool IdentificadoPorCamera);
record FinalizarRequest(string? Justificativa);

// Torna a classe Program visível para os testes de integração (WebApplicationFactory).
public partial class Program { }
