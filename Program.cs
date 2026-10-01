using ScanOPS.Instrumentos;
using ScanOPS.Pessoas;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("front", p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:5174")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("front");

// ===================== USUÁRIOS (login) =====================

var usuario = new Usuario { Nome = "Dr. Teste", Email = "medico@scanops.com", Cargo = TipoUsuario.Cirurgiao, Crm = "12345" };
usuario.AlterarSenha("123456");
var usuarios = new List<Usuario> { usuario };

app.MapGet("/api/teste", () => new { mensagem = "API funcionando!" });

app.MapPost("/api/login", (LoginRequest dados) =>
{
    var encontrado = usuarios.FirstOrDefault(u => u.Autenticar(dados.Email, dados.Senha));

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

var pinca = new Instrumento { Nome = "Pinça Kelly", Codigo = "P001", Tipo = TipoInstrumento.Pinca, PesoReferencia = 50m };
var tesoura = new Instrumento { Nome = "Tesoura Mayo", Codigo = "T001", Tipo = TipoInstrumento.Tesoura, PesoReferencia = 80m };
var instrumentos = new List<Instrumento> { pinca, tesoura };

app.MapGet("/api/instrumentos", () => instrumentos);

app.MapGet("/api/instrumentos/{id}", (Guid id) =>
{
    var item = instrumentos.FirstOrDefault(i => i.Id == id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapPost("/api/instrumentos", (Instrumento novo) =>
{
    instrumentos.Add(novo);
    return Results.Created($"/api/instrumentos/{novo.Id}", novo);
});

app.MapPut("/api/instrumentos/{id}", (Guid id, Instrumento dados) =>
{
    var item = instrumentos.FirstOrDefault(i => i.Id == id);
    if (item is null) return Results.NotFound();

    item.Editar(dados.Nome, dados.Material, dados.Tamanho);
    item.Codigo = dados.Codigo;
    item.Tipo = dados.Tipo;
    item.PesoReferencia = dados.PesoReferencia;

    return Results.Ok(item);
});

app.MapDelete("/api/instrumentos/{id}", (Guid id) =>
{
    var item = instrumentos.FirstOrDefault(i => i.Id == id);
    if (item is null) return Results.NotFound();

    instrumentos.Remove(item);
    return Results.NoContent();
});

// ===================== PACIENTES (CRUD) =====================

var pacientes = new List<Paciente>();

app.MapGet("/api/pacientes", () => pacientes);

app.MapGet("/api/pacientes/{id}", (Guid id) =>
{
    var item = pacientes.FirstOrDefault(p => p.Id == id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapPost("/api/pacientes", (Paciente novo) =>
{
    pacientes.Add(novo);
    return Results.Created($"/api/pacientes/{novo.Id}", novo);
});

app.MapPut("/api/pacientes/{id}", (Guid id, Paciente dados) =>
{
    var item = pacientes.FirstOrDefault(p => p.Id == id);
    if (item is null) return Results.NotFound();

    item.EditarDados(dados.Nome, dados.Alergias, dados.Observacoes);
    item.DataNascimento = dados.DataNascimento;
    item.Sexo = dados.Sexo;
    item.TipoSanguineo = dados.TipoSanguineo;

    return Results.Ok(item);
});

app.MapDelete("/api/pacientes/{id}", (Guid id) =>
{
    var item = pacientes.FirstOrDefault(p => p.Id == id);
    if (item is null) return Results.NotFound();

    pacientes.Remove(item);
    return Results.NoContent();
});

app.Run();

// ===================== REGISTROS AUXILIARES =====================

record LoginRequest(string Email, string Senha);