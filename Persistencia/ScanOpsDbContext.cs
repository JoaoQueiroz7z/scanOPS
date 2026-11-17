using Microsoft.EntityFrameworkCore;
using ScanOPS.Cirurgias;
using ScanOPS.Contagens;
using ScanOPS.Instrumentos;
using ScanOPS.Pessoas;

namespace ScanOPS.Persistencia;

/// <summary>
/// Mapeamento das classes de domínio para o banco SQLite (EF Core).
/// O domínio não conhece o EF: toda a configuração fica aqui.
/// </summary>
public class ScanOpsDbContext : DbContext
{
    public ScanOpsDbContext(DbContextOptions<ScanOpsDbContext> opcoes) : base(opcoes) { }

    public DbSet<Instrumento> Instrumentos => Set<Instrumento>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Procedimento> Procedimentos => Set<Procedimento>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Procedimento>(p =>
        {
            p.HasKey(x => x.Id);
            p.HasOne(x => x.Paciente).WithMany().HasForeignKey("PacienteId").OnDelete(DeleteBehavior.Restrict);
            p.HasOne(x => x.Cirurgiao).WithMany().HasForeignKey("CirurgiaoId").OnDelete(DeleteBehavior.Restrict);
            p.HasOne(x => x.Instrumentista).WithMany().HasForeignKey("InstrumentistaId").OnDelete(DeleteBehavior.Restrict);

            // Composição: os itens pertencem ao procedimento. O EF lê e grava pela lista
            // privada _itens (a propriedade pública Itens é somente leitura) e apaga os
            // itens junto com o procedimento.
            p.HasMany(x => x.Itens).WithOne().HasForeignKey("ProcedimentoId").OnDelete(DeleteBehavior.Cascade);
            p.Navigation(x => x.Itens).HasField("_itens").UsePropertyAccessMode(PropertyAccessMode.Field);

            // RN7 (proteção extra): no máximo um procedimento EmAndamento por sala.
            // Índice único PARCIAL: só vale para as linhas com Status = EmAndamento (valor 1 do enum).
            p.HasIndex(x => x.Sala)
                .IsUnique()
                .HasFilter($"\"Status\" = {(int)StatusProcedimento.EmAndamento}")
                .HasDatabaseName("IX_Procedimentos_Sala_EmAndamento");
        });

        modelo.Entity<ItemProcedimento>(i =>
        {
            i.ToTable("ItensProcedimento");
            i.HasKey(x => x.Id);
            i.Ignore(x => x.Diferenca); // calculada, não é gravada
            // Instrumento usado em procedimento não pode ser apagado (histórico cirúrgico).
            i.HasOne(x => x.Instrumento).WithMany().HasForeignKey("InstrumentoId").OnDelete(DeleteBehavior.Restrict);
        });
    }
}
