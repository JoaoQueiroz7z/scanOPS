namespace ScanOPS.Hardware;

public class BalancaSimulada : IBalanca
{
    private decimal _peso;

    public void DefinirPeso(decimal peso)
    {
        _peso = peso;
    }

    public decimal Pesar() => _peso;

    public void Calibrar()
    {
        _peso = 0;
    }

    public bool VerificarConexao() => true;
}