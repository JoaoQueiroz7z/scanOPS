namespace ScanOPS.Excecoes;

/// <summary>
/// Violação de regra de negócio: o pedido é previsível e foi recusado porque
/// a regra não permite (ex.: contagem em procedimento finalizado).
/// Não é um defeito do sistema; a mensagem é segura para mostrar ao usuário.
/// A API responde 400 (Bad Request).
/// </summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}
