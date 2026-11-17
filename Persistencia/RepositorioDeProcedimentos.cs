using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScanOPS.Cirurgias;
using ScanOPS.Excecoes;

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

    public IReadOnlyList<Procedimento> ListarPorStatus(params StatusProcedimento[] status) =>
        Completos().Where(p => status.Contains(p.Status)).ToList();

    public Procedimento? ObterEmAndamentoNaSala(string sala, Guid exceto)
    {
        var chave = sala.ToLower();
        return Completos().FirstOrDefault(p =>
            p.Status == StatusProcedimento.EmAndamento && p.Id != exceto && p.Sala.ToLower() == chave);
    }

    public void Adicionar(Procedimento procedimento) => _db.Procedimentos.Add(procedimento);

    public void Salvar()
    {
        try
        {
            _db.SaveChanges();
        }
        // Erro 19 = violação de restrição. O SQLite cita as colunas do índice: "UNIQUE constraint failed: Procedimentos.Sala".
        catch (DbUpdateException erro) when (erro.InnerException is SqliteException { SqliteErrorCode: 19 } sqlite
                                             && sqlite.Message.Contains("Procedimentos.Sala"))
        {
            // Proteção extra da RN7: dois inícios ao mesmo tempo na mesma sala. O serviço de domínio
            // já recusa o caso normal; aqui o banco barra a corrida e o erro vira regra, não 500.
            throw new RegraDeNegocioException("A sala já está ocupada por outro procedimento em andamento.");
        }
    }
}
