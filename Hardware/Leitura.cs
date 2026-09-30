namespace ScanOPS.Hardware;

public class Leitura
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DataHora { get; set; } = DateTime.Now;
    public OrigemLeitura Origem { get; set; }
    public string Valor { get; set; } = "";
}