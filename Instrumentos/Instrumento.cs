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
    public bool Ativo { get; set; } = true;
    public decimal PesoReferencia { get; set; }

    public void Editar(string nome, string material, string tamanho)
    {
        Nome = nome;
        Material = material;
        Tamanho = tamanho;
    }

    public void Inativar()
    {
        Ativo = false;
    }
}