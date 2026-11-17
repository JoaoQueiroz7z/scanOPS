using ScanOPS.Cirurgias;
using ScanOPS.Instrumentos;
using ScanOPS.Pessoas;

namespace ScanOPS.Tests.Dominio;

/// <summary>O que o domínio registra para o histórico, e por que ele não pode ser alterado depois.</summary>
public class HistoricoDominioTests
{
    private static Procedimento Planejado(string sala = "Sala 2")
    {
        var medico = new Usuario { Nome = "Dr. Teste", Cargo = TipoUsuario.Cirurgiao };
        var procedimento = new Procedimento("Hernioplastia", sala, "descrição", new Paciente { Nome = "Ana" }, medico, medico);
        procedimento.AdicionarInstrumento(new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m }, 2);
        return procedimento;
    }

    [Fact]
    public void Construtor_PadronizaASala()
    {
        Assert.Equal("Sala 2", Planejado(" sala 02 ").Sala);
    }

    [Fact]
    public void Cancelar_RegistraMotivoEHora()
    {
        var procedimento = Planejado();

        procedimento.Cancelar(" Paciente transferido. ");

        Assert.Equal("Paciente transferido.", procedimento.MotivoCancelamento);
        Assert.NotNull(procedimento.DataHoraFim);
        Assert.Null(procedimento.Justificativa);
    }

    [Fact]
    public void Finalizar_ComDivergencia_RegistraJustificativa_SemDivergencia_Nao()
    {
        var comDivergencia = Planejado();
        comDivergencia.Iniciar();
        comDivergencia.RegistrarEntrada(comDivergencia.Itens[0].Id, 2);
        comDivergencia.RegistrarSaida(comDivergencia.Itens[0].Id, 1, 50m, true);
        comDivergencia.Finalizar("Pinça na mesa auxiliar.");

        var semDivergencia = Planejado();
        semDivergencia.Iniciar();
        semDivergencia.Finalizar();

        Assert.Equal("Pinça na mesa auxiliar.", comDivergencia.Justificativa);
        Assert.Null(semDivergencia.Justificativa);
    }

    [Theory]
    [InlineData(nameof(Procedimento.Id))]
    [InlineData(nameof(Procedimento.Tipo))]
    [InlineData(nameof(Procedimento.Descricao))]
    [InlineData(nameof(Procedimento.Sala))]
    [InlineData(nameof(Procedimento.Paciente))]
    [InlineData(nameof(Procedimento.Cirurgiao))]
    [InlineData(nameof(Procedimento.Instrumentista))]
    [InlineData(nameof(Procedimento.Justificativa))]
    [InlineData(nameof(Procedimento.MotivoCancelamento))]
    [InlineData(nameof(Procedimento.DataHoraInicio))]
    [InlineData(nameof(Procedimento.DataHoraFim))]
    public void DadosDoProcedimento_NaoTemSetterPublico(string propriedade)
    {
        // Sem setter público, um procedimento do histórico não pode ser alterado por fora.
        Assert.Null(typeof(Procedimento).GetProperty(propriedade)!.GetSetMethod(nonPublic: false));
    }

    [Fact]
    public void Foto_ItemGuardaNomeEPesoDoMomentoDoPlanejamento()
    {
        var pinca = new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m };
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(pinca, 2);

        pinca.Editar("Pinça Kelly Curva", "", "");
        pinca.PesoReferencia = 55m;

        var item = procedimento.Itens[0];
        Assert.Equal("Pinça Kelly", item.NomeInstrumento);
        Assert.Equal(50m, item.PesoReferencia);
    }

    [Fact]
    public void Foto_RN4UsaOPesoDaFoto()
    {
        var pinca = new Instrumento { Nome = "Pinça Kelly", PesoReferencia = 50m };
        var procedimento = new Procedimento();
        procedimento.AdicionarInstrumento(pinca, 2);
        procedimento.Iniciar();
        var item = procedimento.Itens[0];
        procedimento.RegistrarEntrada(item.Id, 2);
        procedimento.RegistrarSaida(item.Id, 2, 100m, true);

        pinca.PesoReferencia = 80m; // cadastro corrigido no meio da cirurgia

        Assert.Equal(100m, item.PesoEsperado()); // 50 g da foto × 2
        Assert.True(item.ConferePeso());
    }
}
