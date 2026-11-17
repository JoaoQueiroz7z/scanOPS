using System.Text.RegularExpressions;
using ScanOPS.Excecoes;

namespace ScanOPS.Cirurgias;

/// <summary>
/// Padroniza o nome da sala para que "Sala 1", "sala 1", " 1 " e "01" sejam a mesma sala.
/// É o formato gravado no banco e mostrado nas telas ("Sala 1"), e é a base da RN7
/// (uma sala só pode ter um procedimento em andamento).
/// </summary>
public static partial class SalaCirurgica
{
    [GeneratedRegex(@"^sala\b\s*", RegexOptions.IgnoreCase)]
    private static partial Regex PrefixoSala();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();

    /// <summary>"sala 02" -> "Sala 2"; "Híbrida" -> "Sala Híbrida". Vazio é recusado.</summary>
    public static string Padronizar(string? sala)
    {
        var texto = Espacos().Replace(sala ?? "", " ").Trim();
        var resto = PrefixoSala().Replace(texto, "").Trim();

        if (resto.Length == 0)
            throw new RegraDeNegocioException("Informe a sala do procedimento.");

        // Número: sem zeros à esquerda ("02" e "2" são a mesma sala).
        if (resto.All(char.IsDigit))
            resto = resto.TrimStart('0') is { Length: > 0 } semZeros ? semZeros : "0";

        return $"Sala {resto}";
    }

    /// <summary>Compara duas salas já padronizadas, sem diferenciar maiúsculas ("Sala UTI" = "Sala uti").</summary>
    public static bool MesmaSala(string a, string b) =>
        string.Equals(Padronizar(a), Padronizar(b), StringComparison.OrdinalIgnoreCase);
}
