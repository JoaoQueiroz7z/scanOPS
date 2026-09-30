using ScanOPS.Cirurgias;
using ScanOPS.Pessoas;

namespace ScanOPS.Relatorios;

public class Relatorio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DataGeracao { get; set; } = DateTime.Now;
    public int TotalInstrumentos { get; set; }
    public bool Divergencia { get; set; }
    public string Observacoes { get; set; } = "";
    public StatusAssinatura StatusAssinatura { get; private set; } = StatusAssinatura.Pendente;
    public string Arquivo { get; set; } = "";

    public Procedimento Procedimento { get; set; } = null!;

    public void GerarPDF()
    {
        throw new NotImplementedException("Falta implementar a geração do PDF.");
    }

    public void AssinarCirurgiao(Usuario cirurgiao)
    {
        if (cirurgiao.Cargo != TipoUsuario.Cirurgiao)
            throw new InvalidOperationException("Somente um cirurgião pode assinar aqui.");

        StatusAssinatura = StatusAssinatura == StatusAssinatura.AssinadoInstrumentista
            ? StatusAssinatura.AssinadoCompleto
            : StatusAssinatura.AssinadoCirurgiao;
    }

    public void AssinarInstrumentista(Usuario instrumentista)
    {
        if (instrumentista.Cargo != TipoUsuario.Instrumentista)
            throw new InvalidOperationException("Somente um instrumentista pode assinar aqui.");

        StatusAssinatura = StatusAssinatura == StatusAssinatura.AssinadoCirurgiao
            ? StatusAssinatura.AssinadoCompleto
            : StatusAssinatura.AssinadoInstrumentista;
    }
}