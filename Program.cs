var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("front", p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:5174")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("front");

app.MapGet("/api/teste", () => new { mensagem = "API funcionando!" });

app.Run();