namespace ScanOPS.Hardware;

public interface ICamera
{
    string Capturar();
    List<LeituraCamera> Escanear();
    bool VerificarConexao();
}