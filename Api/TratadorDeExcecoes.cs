using Microsoft.AspNetCore.Diagnostics;
using ScanOPS.Excecoes;
using ScanOPS.Hardware;

namespace ScanOPS.Api;

/// <summary>
/// Tratamento central de exceções da API. Todas as rotas lançam e este é o
/// único lugar que decide o código HTTP e o texto devolvido ao usuário.
/// Nunca devolve stack trace nem mensagens internas de falhas excepcionais.
/// </summary>
public class TratadorDeExcecoes : IExceptionHandler
{
    public const string MensagemHardwareIndisponivel =
        "Equipamento de medição indisponível. Verifique a conexão ou faça a conferência manual.";

    public const string MensagemRequisicaoInvalida =
        "Os dados enviados não estão no formato esperado.";

    public const string MensagemErroInesperado =
        "Ocorreu um erro inesperado. Tente novamente mais tarde.";

    private readonly ILogger<TratadorDeExcecoes> _logger;

    public TratadorDeExcecoes(ILogger<TratadorDeExcecoes> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken cancelamento)
    {
        var (status, mensagem) = excecao switch
        {
            // Violação de regra: mensagem do domínio é segura e útil para o usuário.
            RegraDeNegocioException regra => (StatusCodes.Status400BadRequest, regra.Message),

            // Recurso pedido não existe (ex.: id de item inválido).
            KeyNotFoundException naoEncontrado => (StatusCodes.Status404NotFound, naoEncontrado.Message),

            // JSON fora do formato do DTO (ex.: texto onde se espera número): erro do cliente, não do servidor.
            BadHttpRequestException requisicaoInvalida => (requisicaoInvalida.StatusCode, MensagemRequisicaoInvalida),

            // Situação excepcional de hardware: detalhe só no log.
            FalhaDeHardwareException => (StatusCodes.Status503ServiceUnavailable, MensagemHardwareIndisponivel),

            // Qualquer outra coisa é defeito: detalhe só no log.
            _ => (StatusCodes.Status500InternalServerError, MensagemErroInesperado)
        };

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(excecao, "Falha ao processar {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);
        else
            _logger.LogWarning("Pedido recusado em {Metodo} {Caminho}: {Mensagem}", contexto.Request.Method, contexto.Request.Path, excecao.Message);

        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(new { erro = mensagem }, cancelamento);
        return true;
    }
}
