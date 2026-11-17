using ScanOPS.Cirurgias;
using ScanOPS.Instrumentos;
using ScanOPS.Pessoas;

namespace ScanOPS.Persistencia;

/// <summary>
/// Prepara o banco local na inicialização da API: cria o arquivo SQLite e as
/// tabelas se ainda não existirem e, se o banco estiver vazio, grava os dados de
/// demonstração (usuário, dois instrumentos e um procedimento em andamento).
/// Para recomeçar do zero, basta apagar o arquivo scanops.db.
/// </summary>
public static class InicializadorDoBanco
{
    public static void Inicializar(ScanOpsDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Usuarios.Any())
            return;

        var medico = new Usuario { Nome = "Dr. Teste", Email = "medico@scanops.com", Cargo = TipoUsuario.Cirurgiao, Crm = "12345" };
        medico.AlterarSenha("123456");

        var pinca = new Instrumento { Nome = "Pinça Kelly", Codigo = "P001", Tipo = TipoInstrumento.Pinca, PesoReferencia = 50m };
        var tesoura = new Instrumento { Nome = "Tesoura Mayo", Codigo = "T001", Tipo = TipoInstrumento.Tesoura, PesoReferencia = 80m };

        var procedimento = new Procedimento("Apendicectomia", "Sala 1", null,
            new Paciente { Nome = "Paciente Demo" }, cirurgiao: medico, instrumentista: medico);
        procedimento.AdicionarInstrumento(pinca, 2);
        procedimento.AdicionarInstrumento(tesoura, 1);
        procedimento.Iniciar();

        db.Usuarios.Add(medico);
        db.Instrumentos.AddRange(pinca, tesoura);
        db.Procedimentos.Add(procedimento);
        db.SaveChanges();
    }
}
