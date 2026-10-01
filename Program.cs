using ScanOPS.Pessoas;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("front", p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:5174")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("front");

// Lista de usuários de teste (depois vira banco de dados)
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

app.Run();

// Formato que o front vai enviar
record LoginRequest(string Email, string Senha);