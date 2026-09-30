using ScanOPS.Cirurgias;
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

        var procedimento = new Procedimento
        {
            Tipo = "Apendicectomia",
            Sala = "Sala 1",
            Paciente = new Paciente { Nome = "Paciente Teste" },
            Cirurgiao = cirurgiao,
            Instrumentista = instrumentista
        };

        procedimento.AdicionarInstrumento(pinca, 2);
        procedimento.AdicionarInstrumento(tesoura, 1);
        procedimento.Iniciar();

        var balanca = new BalancaSimulada();
        var camera = new CameraSimulada();

        procedimento.Itens[0].RegistrarEntrada(2);
        procedimento.Itens[1].RegistrarEntrada(1);

        balanca.DefinirPeso(100m);
        camera.DefinirLeituras(new LeituraCamera("P001", 0.97), new LeituraCamera("P001", 0.95));

        procedimento.Itens[0].RegistrarSaida(2);
        procedimento.Itens[0].PesoMedido = balanca.Pesar();
        procedimento.Itens[0].IdentificadoPorCamera = camera.Escanear().Count > 0;

        procedimento.Itens[1].RegistrarSaida(0);
        procedimento.Itens[1].PesoMedido = 0m;

        try
        {
            procedimento.Finalizar();
        }
        catch (InvalidOperationException erro)
        {
            Console.WriteLine($"ALERTA: {erro.Message}");
        }

        foreach (var item in procedimento.ObterDiferencas())
            Console.WriteLine($"Faltando: {item.Instrumento.Nome} (diferença: {item.Diferenca})");

        procedimento.Finalizar("Tesoura localizada fora da sala, conferida manualmente.");
        Console.WriteLine($"Status final: {procedimento.Status}");

        var relatorio = procedimento.GerarRelatorio();
        Console.WriteLine($"Relatório: {relatorio.TotalInstrumentos} instrumentos, divergência: {relatorio.Divergencia}");
    }
}