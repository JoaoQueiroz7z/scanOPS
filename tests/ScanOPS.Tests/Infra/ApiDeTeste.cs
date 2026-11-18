using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
        // Pooling=False: cada conexão é fechada de verdade ao terminar, então o arquivo pode ser
        // apagado sem precisar de SqliteConnection.ClearAllPools(). Esse método é GLOBAL: limpava
        // as conexões de todos os bancos do processo, inclusive as de outros testes rodando em
        // paralelo, que falhavam com ObjectDisposedException (erro 500) de vez em quando.
        builder.UseSetting("ConnectionStrings:ScanOps", $"Data Source={ArquivoDoBanco};Pooling=False");
        _configuracaoExtra?.Invoke(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(ArquivoDoBanco))
                File.Delete(ArquivoDoBanco);
        }
        catch (IOException)
        {
            // Limpeza é só arrumação da pasta temporária: nunca deve derrubar um teste.
        }
    }
}
