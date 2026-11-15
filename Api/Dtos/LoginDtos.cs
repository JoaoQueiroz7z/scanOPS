using ScanOPS.Pessoas;

namespace ScanOPS.Api.Dtos;

public record LoginRequisicao(string Email, string Senha);

/// <summary>Dados do usuário autenticado. Nunca inclui o hash da senha.</summary>
public record LoginResposta(Guid Id, string Nome, string Cargo)
{
    public static LoginResposta De(Usuario u) => new(u.Id, u.Nome, u.Cargo.ToString());
}
