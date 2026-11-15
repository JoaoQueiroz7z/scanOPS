using ScanOPS.Cirurgias;
using ScanOPS.Hardware;

namespace ScanOPS.Servicos;

/// <summary>
/// Liga o hardware (balança) ao domínio (Procedimento). Depende apenas do
/// contrato IBalanca, então funciona com qualquer balança registrada.
/// </summary>
public class ServicoDePesagem
{
    private readonly IBalanca _balanca;

    public ServicoDePesagem(IBalanca balanca)
    {
        _balanca = balanca;
    }

    public decimal PesarItem(Procedimento procedimento, Guid itemId)
    {
        decimal peso;
        try
        {
            if (!_balanca.VerificarConexao())
                throw new FalhaDeHardwareException("A balança não respondeu à verificação de conexão.");

            peso = _balanca.Pesar();
        }
        catch (FalhaDeHardwareException)
        {
            throw;
        }
        catch (Exception erroDoDriver)
        {
            // Erro de baixo nível do equipamento: preserva o detalhe como exceção interna (para o log).
            throw new FalhaDeHardwareException("Erro ao ler a balança.", erroDoDriver);
        }

        // A regra (procedimento em andamento, peso não negativo) é do domínio.
        procedimento.RegistrarPeso(itemId, peso);
        return peso;
    }
}
