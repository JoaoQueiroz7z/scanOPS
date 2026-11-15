namespace ScanOPS.Hardware;

/// <summary>
/// Escolhe a implementação de IBalanca a partir da configuração (appsettings.json):
///   "Balanca": { "Tipo": "Simulada" | "ComRuido", "AmplitudeRuidoGramas": 1.5 }
/// O resto da aplicação só conhece o contrato IBalanca.
/// </summary>
public static class ConfiguracaoDeHardware
{
    public const string SecaoBalanca = "Balanca";

    public static IServiceCollection AdicionarBalanca(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var secao = configuracao.GetSection(SecaoBalanca);
        var tipo = secao["Tipo"] ?? "Simulada";

        IBalanca balanca = tipo switch
        {
            "Simulada" => new BalancaSimulada(),
            "ComRuido" => new BalancaComRuido(secao.GetValue("AmplitudeRuidoGramas", 1.5m)),
            // Erro de configuração: falha já na inicialização, com mensagem clara.
            _ => throw new InvalidOperationException(
                $"Tipo de balança desconhecido em '{SecaoBalanca}:Tipo': '{tipo}'. Use 'Simulada' ou 'ComRuido'.")
        };

        servicos.AddSingleton(balanca);
        return servicos;
    }
}
