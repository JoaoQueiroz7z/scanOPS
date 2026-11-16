namespace ScanOPS.Instrumentos;

public class Instrumento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = "";
    public string Codigo { get; set; } = "";
    public TipoInstrumento Tipo { get; set; }
    public string Material { get; set; } = "";
    public string Tamanho { get; set; } = "";
    public string Esterilizacao { get; set; } = "";
    // Só muda por Inativar()/Reativar(). Instrumento inativo não entra em procedimento novo (RN6).
    public bool Ativo { get; private set; } = true;
    public decimal PesoReferencia { get; set; }

    public void Editar(string nome, string material, string tamanho)
    {
        Nome = nome;
        Material = material;
        Tamanho = tamanho;
    }

    /// <summary>Tira o instrumento de uso (ex.: danificado). O histórico de procedimentos é mantido.</summary>
    public void Inativar()
    {
        Ativo = false;
    }

    /// <summary>Devolve o instrumento ao uso.</summary>
    public void Reativar()
    {
        Ativo = true;
    }
}