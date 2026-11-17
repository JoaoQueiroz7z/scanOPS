using ScanOPS.Cirurgias;
using ScanOPS.Excecoes;
using ScanOPS.Hardware;
using ScanOPS.Instrumentos;
using ScanOPS.Pessoas;

namespace ScanOPS.Servicos;

public static class Demonstracao
{
    public static void Executar()
    {
        var pinca = new Instrumento { Nome = "Pinça Kelly", Codigo = "P001", Tipo = TipoInstrumento.Pinca, PesoReferencia = 50m };
        var tesoura = new Instrumento { Nome = "Tesoura Mayo", Codigo = "T001", Tipo = TipoInstrumento.Tesoura, PesoReferencia = 80m };

        var cirurgiao = new Usuario { Nome = "Dr. Teste", Cargo = TipoUsuario.Cirurgiao, Crm = "12345" };
        var instrumentista = new Usuario { Nome = "Enf. Teste", Cargo = TipoUsuario.Instrumentista, Coren = "6789" };

        var procedimento = new Procedimento("Apendicectomia", "Sala 1", null,
            new Paciente { Nome = "Paciente Teste" }, cirurgiao, instrumentista);

        procedimento.AdicionarInstrumento(pinca, 2);
        procedimento.AdicionarInstrumento(tesoura, 1);
        procedimento.Iniciar();

        var balanca = new BalancaSimulada();
        var camera = new CameraSimulada();

        var itemPinca = procedimento.Itens[0];
        var itemTesoura = procedimento.Itens[1];

        procedimento.RegistrarEntrada(itemPinca.Id, 2);
        procedimento.RegistrarEntrada(itemTesoura.Id, 1);

        balanca.DefinirPeso(100m);
        camera.DefinirLeituras(new LeituraCamera("P001", 0.97), new LeituraCamera("P001", 0.95));

        procedimento.RegistrarSaida(itemPinca.Id, 2, balanca.Pesar(), camera.Escanear().Count > 0);
        procedimento.RegistrarSaida(itemTesoura.Id, 0, 0m, false);

        try
        {
            procedimento.Finalizar();
        }
        catch (RegraDeNegocioException erro)
        {
            Console.WriteLine($"ALERTA: {erro.Message}");
        }

        foreach (var item in procedimento.ObterDiferencas())
            Console.WriteLine($"Faltando: {item.NomeInstrumento} (diferença: {item.Diferenca})");

        procedimento.Finalizar("Tesoura localizada fora da sala, conferida manualmente.");
        Console.WriteLine($"Status final: {procedimento.Status}");

        var relatorio = procedimento.GerarRelatorio();
        Console.WriteLine($"Relatório: {relatorio.TotalInstrumentos} instrumentos, divergência: {relatorio.Divergencia}");
    }
}