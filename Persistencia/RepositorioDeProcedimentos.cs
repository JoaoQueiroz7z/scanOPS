using Microsoft.EntityFrameworkCore;
using ScanOPS.Cirurgias;

namespace ScanOPS.Persistencia;

public class RepositorioDeProcedimentos : IRepositorioDeProcedimentos
{
    private readonly ScanOpsDbContext _db;

    public RepositorioDeProcedimentos(ScanOpsDbContext db)
    {
        _db = db;
    }

    private IQueryable<Procedimento> Completos() => _db.Procedimentos
        .Include(p => p.Itens.OrderBy(i => i.Sequencia)).ThenInclude(i => i.Instrumento)
        .Include(p => p.Paciente)
        .Include(p => p.Cirurgiao)
        .Include(p => p.Instrumentista);

    public Procedimento? ObterAtual() =>
        Completos().OrderByDescending(p => p.DataHoraInicio).FirstOrDefault();

    public Procedimento? ObterPorId(Guid id) =>
        Completos().FirstOrDefault(p => p.Id == id);

    public void Adicionar(Procedimento procedimento) => _db.Procedimentos.Add(procedimento);

    public void Salvar() => _db.SaveChanges();
}
