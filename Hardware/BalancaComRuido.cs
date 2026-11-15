namespace ScanOPS.Hardware;

/// <summary>
/// Simula uma balança real: cada leitura tem um ruído aleatório (± AmplitudeRuido)
/// e uma deriva que cresce a cada uso (DerivaPorLeitura). Calibrar() zera a deriva.
/// Contrasta com BalancaSimulada, que sempre devolve o peso exato.
/// É por causa desse tipo de imprecisão que a RN4 confere o peso com tolerância.
/// </summary>
public class BalancaComRuido : IBalanca
{
    public const decimal DerivaPorLeitura = 0.05m;

    private readonly Random _aleatorio;
    private decimal _pesoNoPrato;
    private decimal _deriva;

    public decimal AmplitudeRuido { get; }

    /// <param name="amplitudeRuido">Variação máxima, em gramas, para mais ou para menos.</param>
    /// <param name="semente">Semente do gerador aleatório; informe nos testes para resultados repetíveis.</param>
    public BalancaComRuido(decimal amplitudeRuido = 1.5m, int? semente = null)
    {
        if (amplitudeRuido < 0)
            throw new ArgumentOutOfRangeException(nameof(amplitudeRuido), "A amplitude do ruído não pode ser negativa.");

        AmplitudeRuido = amplitudeRuido;
        _aleatorio = semente is null ? new Random() : new Random(semente.Value);
    }

    /// <summary>Simulação: o que está fisicamente sobre o prato.</summary>
    public void DefinirPesoNoPrato(decimal peso)
    {
        _pesoNoPrato = peso;
    }

    public decimal Pesar()
    {
        decimal ruido = ((decimal)_aleatorio.NextDouble() * 2m - 1m) * AmplitudeRuido;
        decimal leitura = _pesoNoPrato + _deriva + ruido;
        _deriva += DerivaPorLeitura;

        // Balança real não mostra peso negativo: com o prato vazio, marca zero.
        return Math.Max(0m, Math.Round(leitura, 1));
    }

    public void Calibrar()
    {
        _deriva = 0m;
    }

    public bool VerificarConexao() => true;
}
