using ScanOPS.Cirurgias;
using ScanOPS.Excecoes;
using ScanOPS.Persistencia;

namespace ScanOPS.Servicos;

/// <summary>
/// Serviço de domínio da RN7: duas cirurgias não podem acontecer na mesma sala ao mesmo tempo.
/// A regra envolve VÁRIOS procedimentos, então um Procedimento sozinho não consegue garanti-la:
/// este serviço consulta o repositório antes de chamar Procedimento.Iniciar().
/// </summary>
public class ServicoDeInicioDeProcedimento
{
    private readonly IRepositorioDeProcedimentos _repositorio;

    public ServicoDeInicioDeProcedimento(IRepositorioDeProcedimentos repositorio)
    {
        _repositorio = repositorio;
    }

    public Procedimento Iniciar(Guid procedimentoId)
    {
        var procedimento = _repositorio.ObterPorId(procedimentoId)
            ?? throw new KeyNotFoundException("Procedimento não encontrado.");

        // Só faz sentido checar a sala de quem pode iniciar; nos outros casos a RN1 responde.
        if (procedimento.Status == StatusProcedimento.Planejado)
        {
            var ocupante = _repositorio.ObterEmAndamentoNaSala(procedimento.Sala, exceto: procedimento.Id);
            if (ocupante is not null)
                throw new RegraDeNegocioException(
                    $"A {procedimento.Sala} já está ocupada pelo procedimento {ocupante.Tipo}. Finalize-o antes de iniciar outro nesta sala.");
        }

        procedimento.Iniciar(); // RN1
        _repositorio.Salvar();
        return procedimento;
    }
}
