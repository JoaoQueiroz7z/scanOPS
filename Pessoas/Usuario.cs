using System.Security.Cryptography;
using System.Text;

namespace ScanOPS.Pessoas;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; private set; } = "";
    public TipoUsuario Cargo { get; set; }
    public bool Ativo { get; set; } = true;
    public string? Crm { get; set; }
    public string? Coren { get; set; }
    public string? Nivel { get; set; }

    public bool Autenticar(string email, string senha)
    {
        return Ativo
            && string.Equals(Email, email, StringComparison.OrdinalIgnoreCase)
            && SenhaHash == GerarHash(senha);
    }

    public void AlterarSenha(string novaSenha)
    {
        SenhaHash = GerarHash(novaSenha);
    }

    public void Desativar()
    {
        Ativo = false;
    }

    private static string GerarHash(string texto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexString(bytes);
    }
}