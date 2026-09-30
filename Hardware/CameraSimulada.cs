namespace ScanOPS.Hardware;

public class CameraSimulada : ICamera
{
    private readonly List<LeituraCamera> _leituras = new();

    public void DefinirLeituras(params LeituraCamera[] leituras)
    {
        _leituras.Clear();
        _leituras.AddRange(leituras);
    }

    public string Capturar() => "imagem_simulada.jpg";

    public List<LeituraCamera> Escanear() => new(_leituras);

    public bool VerificarConexao() => true;
}