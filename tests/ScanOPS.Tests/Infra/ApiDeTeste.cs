using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace ScanOPS.Tests.Infra;

/// <summary>
/// Sobe a API inteira em memória (WebApplicationFactory) apontando para um arquivo
/// SQLite temporário e exclusivo. Assim cada teste começa com o banco recém-criado
/// e com os dados de demonstração, sem interferir nos outros testes nem no
/// scanops.db de desenvolvimento. O arquivo é apagado ao final.
/// </summary>
public class ApiDeTeste : WebApplicationFactory<Program>
{
    private readonly Action<IWebHostBuilder>? _configuracaoExtra;

    public string ArquivoDoBanco { get; } =
        Path.Combine(Path.GetTempPath(), $"scanops-teste-{Guid.NewGuid():N}.db");

    public ApiDeTeste(Action<IWebHostBuilder>? configuracaoExtra = null)
    {
        _configuracaoExtra = configuracaoExtra;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ScanOps", $"Data Source={ArquivoDoBanco}");
        _configuracaoExtra?.Invoke(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools(); // libera o arquivo para poder apagar
        if (File.Exists(ArquivoDoBanco))
            File.Delete(ArquivoDoBanco);
    }
}
