using ScanOPS.Instrumentos;

namespace ScanOPS.Contagens;

public class ItemProcedimento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Instrumento Instrumento { get; set; } = null!;
    public int QuantidadePrevista { get; set; }
    public int QuantidadeInicial { get; private set; }
    public int QuantidadeFinal { get; private set; }
    public int Diferenca => CalcularDiferenca();
    public decimal PesoMedido { get; set; }
    public bool IdentificadoPorCamera { get; set; }
    public decimal ToleranciaPeso { get; set; } = 2m;
    public StatusItem Status { get; private set; } = StatusItem.Pendente;

    public void RegistrarEntrada(int qtd)
    {
        QuantidadeInicial += qtd;
    }

    public void RegistrarSaida(int qtd)
    {
        QuantidadeFinal += qtd;
    }

    public int CalcularDiferenca()
    {
        return QuantidadeInicial - QuantidadeFinal;
    }

    public bool ConferePeso()
    {
        decimal esperado = Instrumento.PesoReferencia * QuantidadeInicial;
        return Math.Abs(PesoMedido - esperado) <= ToleranciaPeso;
    }

    public void Conferir()
    {
        bool tudoOk = CalcularDiferenca() == 0 && ConferePeso();
        Status = tudoOk ? StatusItem.Conferido : StatusItem.Divergente;
    }
}