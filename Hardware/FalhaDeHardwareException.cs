namespace ScanOPS.Hardware;

/// <summary>
/// Situação excepcional: um equipamento (balança ou câmera) não respondeu.
/// Não depende do usuário nem de regra de negócio. A mensagem e a exceção
/// interna podem conter detalhes técnicos, por isso vão só para o log;
/// a API responde 503 (Service Unavailable) com uma mensagem genérica.
/// </summary>
public class FalhaDeHardwareException : Exception
{
    public FalhaDeHardwareException(string mensagem, Exception? interna = null)
        : base(mensagem, interna) { }
}
