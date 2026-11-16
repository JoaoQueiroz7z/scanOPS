using ScanOPS.Instrumentos;

namespace ScanOPS.Contagens;

/// <summary>
/// Item contado dentro de um Procedimento (parte do agregado Procedimento).
/// Só pode ser criado e alterado pelo próprio Procedimento: construtor, setters
/// e métodos de alteração são internal. Leitura continua pública.
/// </summary>
public class ItemProcedimento
{
    public const decimal ToleranciaPesoPadrao = 2m;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public int Sequencia { get; private set; }
    public Instrumento Instrumento { get; private set; }
    public int QuantidadePrevista { get; private set; }
    public int QuantidadeInicial { get; private set; }
    public int QuantidadeFinal { get; private set; }
    public int Diferenca => CalcularDiferenca();
    public decimal PesoMedido { get; private set; }
    public bool IdentificadoPorCamera { get; private set; }
    public decimal ToleranciaPeso { get; private set; } = ToleranciaPesoPadrao;
    public StatusItem Status { get; private set; } = StatusItem.Pendente;

    internal ItemProcedimento(Instrumento instrumento, int quantidadePrevista, int sequencia)
    {
        Instrumento = instrumento;
        QuantidadePrevista = quantidadePrevista;
        Sequencia = sequencia;
    }

    // Usado apenas pelo EF Core para recriar o objeto a partir do banco.
    private ItemProcedimento()
    {
        Instrumento = null!;
    }

    internal void RegistrarEntrada(int qtd)
    {
        QuantidadeInicial += qtd;
    }

    internal void RegistrarSaida(int qtd)
    {
        QuantidadeFinal += qtd;
    }

    internal void RegistrarPeso(decimal peso)
    {
        PesoMedido = peso;
    }

    internal void RegistrarIdentificacaoPorCamera(bool identificado)
    {
        IdentificadoPorCamera = identificado;
    }

    public int CalcularDiferenca()
    {
        return QuantidadeInicial - QuantidadeFinal;
    }

    /// <summary>Peso que deveria voltar: peso de referência × unidades que entraram em campo.</summary>
    public decimal PesoEsperado()
    {
        return Instrumento.PesoReferencia * QuantidadeInicial;
    }

    // RN4: o peso medido precisa bater com o peso de referência esperado, dentro da tolerância.
    public bool ConferePeso()
    {
        return Math.Abs(PesoMedido - PesoEsperado()) <= ToleranciaPeso;
    }

    public bool PossuiDivergencia()
    {
        return CalcularDiferenca() != 0 || !ConferePeso();
    }

    internal void Conferir()
    {
        Status = PossuiDivergencia() ? StatusItem.Divergente : StatusItem.Conferido;
    }
}
