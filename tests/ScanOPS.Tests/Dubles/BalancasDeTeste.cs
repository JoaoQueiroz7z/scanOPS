using ScanOPS.Hardware;

namespace ScanOPS.Tests.Dubles;

/// <summary>Dublê de teste: balança desligada (não responde à verificação de conexão).</summary>
public class BalancaDesconectada : IBalanca
{
    public bool PesarFoiChamado { get; private set; }

    public decimal Pesar()
    {
        PesarFoiChamado = true;
        return 0m;
    }

    public void Calibrar() { }

    public bool VerificarConexao() => false;
}

/// <summary>Dublê de teste: balança conectada, mas o "driver" falha na leitura.</summary>
public class BalancaComErroDeLeitura : IBalanca
{
    public decimal Pesar() => throw new IOException("COM3: timeout ao ler a porta serial");

    public void Calibrar() { }

    public bool VerificarConexao() => true;
}
