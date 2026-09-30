namespace ScanOPS.Hardware;

public interface IBalanca
{
    decimal Pesar();
    void Calibrar();
    bool VerificarConexao();
}