using ScanOPS.Cirurgias;

namespace ScanOPS.Persistencia;

/// <summary>
/// Acesso ao agregado Procedimento. As rotas dependem desta interface, não do EF:
/// o procedimento sempre volta completo (itens, instrumentos e pessoas) e as
/// alterações feitas pelos métodos de domínio são gravadas em Salvar().
/// </summary>
public interface IRepositorioDeProcedimentos
{
    /// <summary>Procedimento mais recente (o "procedimento atual" da sala), ou null.</summary>
    Procedimento? ObterAtual();

    Procedimento? ObterPorId(Guid id);

    void Adicionar(Procedimento procedimento);

    void Salvar();
}
