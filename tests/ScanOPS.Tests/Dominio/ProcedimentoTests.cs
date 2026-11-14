using ScanOPS.Cirurgias;
using ScanOPS.Instrumentos;

namespace ScanOPS.Tests.Dominio;

public class ProcedimentoTests
{
    [Fact]
    public void NovoProcedimento_ComecaPlanejado()
    {
        var procedimento = new Procedimento();

        Assert.Equal(StatusProcedimento.Planejado, procedimento.Status);
        Assert.Null(procedimento.DataHoraInicio);
    }

    [Fact]
    public void Iniciar_ProcedimentoPlanejado_PassaParaEmAndamento()
    {
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m }, 2);

        procedimento.Iniciar();

        Assert.Equal(StatusProcedimento.EmAndamento, procedimento.Status);
        Assert.NotNull(procedimento.DataHoraInicio);
    }
}
