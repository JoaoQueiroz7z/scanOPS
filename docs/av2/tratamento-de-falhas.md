# Tratamento de falhas e exceções

## 1. Três situações de falha (D01)

O sistema separa **violação de regra de negócio** (o pedido é previsível e a regra não permite;
a mensagem é útil ao usuário) de **situação excepcional** (algo fora do controle do usuário e das
regras; os detalhes técnicos não devem chegar ao usuário).

| # | Situação | Tipo | Exceção | Resposta HTTP | Comportamento esperado |
|---|---|---|---|---|---|
| F1 | Finalizar procedimento com divergência e sem justificativa | Regra de negócio (RN3) | `RegraDeNegocioException` | **400** `{ "erro": "Há divergências na contagem. Informe uma justificativa para finalizar." }` | Procedimento continua `EmAndamento`; nenhum item muda de status; nada é gravado |
| F2 | Registrar contagem (entrada/saída/peso) em procedimento que não está em andamento, ou com quantidade inválida | Regra de negócio (RN5) | `RegraDeNegocioException` | **400** `{ "erro": "Só é possível registrar contagem em procedimento em andamento." }` (ou a mensagem da quantidade) | Contagem não muda, no objeto e no banco |
| F3 | Balança indisponível (não responde ou erro do driver) | Excepcional | `FalhaDeHardwareException` (com a exceção original em `InnerException`) | **503** `{ "erro": "Equipamento de medição indisponível. Verifique a conexão ou faça a conferência manual." }` | Peso do item não muda; detalhe técnico vai só para o log; nenhum stack trace na resposta |

Outros casos tratados pelo mesmo mecanismo: recurso inexistente (`KeyNotFoundException` → 404),
JSON fora do formato (`BadHttpRequestException` → 400 com mensagem genérica) e qualquer erro não
previsto (→ 500 com mensagem genérica, detalhe no log).

Validações que **não** usam exceção: login inválido devolve `401` diretamente
(`Results.Unauthorized()`), porque errar a senha é um resultado esperado, não uma falha.

## 2. Caminho da exceção até o tratamento (D02)

```
Rota em Program.cs (sem try/catch)
  └─ procedimento.Finalizar(justificativa)        Cirurgias/Procedimento.cs  → lança RegraDeNegocioException
       ↑ propaga
  └─ ServicoDePesagem.PesarItem(...)              Servicos/ServicoDePesagem.cs → lança FalhaDeHardwareException
       ↑ propaga
Middleware UseExceptionHandler()                  Program.cs
  └─ TratadorDeExcecoes.TryHandleAsync(...)       Api/TratadorDeExcecoes.cs
       ├─ escolhe o código HTTP e a mensagem segura
       ├─ registra no log (Warning para regra; Error com exceção completa para falha excepcional)
       └─ escreve { "erro": "..." }
```

Os dados não ficam inconsistentes porque (1) o domínio valida antes de alterar e (2) a rota só
chama `repositorio.Salvar()` depois que o método de domínio terminou sem exceção.

Execução real registrada em `docs/av2/evidencias/excecoes-api.txt` (respostas HTTP e linhas de log
do `TratadorDeExcecoes`).

## 3. Falha excepcional sem vazar detalhes (D03)

- A mensagem técnica (`"COM3: timeout ao ler a porta serial"` no dublê de teste) fica em
  `FalhaDeHardwareException.InnerException` e é registrada com `LogError(excecao, ...)`.
- O usuário recebe só `TratadorDeExcecoes.MensagemHardwareIndisponivel`.
- O teste `F3_Api_ErroNaBalanca_Retorna503ComMensagemGenericaSemDetalhesInternos` verifica que a
  resposta **não contém** `COM3`, `Exception` nem ` at ` (linha de stack trace).
- A falha **não é ocultada**: ela gera 503 (não 200) e fica no log.

## 4. Testes das três situações (D04)

Arquivo `tests/ScanOPS.Tests/Excecoes/SituacoesDeFalhaTests.cs` — cada situação testada no domínio
(exceção e estado) e na fronteira HTTP (código e mensagem):

| Situação | Domínio / serviço | API |
|---|---|---|
| F1 | `F1_Dominio_FinalizarComDivergenciaSemJustificativa_LancaRegraEContinuaEmAndamento` | `F1_Api_FinalizarComDivergenciaSemJustificativa_Retorna400ComMensagemDaRegra` |
| F2 | `F2_Dominio_RegistrarSaidaAposFinalizar_LancaRegraENaoAlteraItem` | `F2_Api_RegistrarSaidaAposFinalizar_Retorna400ComMensagemDaRegra` |
| F3 | `F3_Servico_BalancaDesconectada_LancaFalhaDeHardwareENaoAlteraPeso`, `F3_Servico_ErroDoDriver_EhEmbrulhadoEmFalhaDeHardwarePreservandoACausa` | `F3_Api_ErroNaBalanca_Retorna503ComMensagemGenericaSemDetalhesInternos` |

Complementares: `JsonComTipoErrado_Retorna400ComMensagemGenericaENao500` (`ContratoApiTests.cs`) e
`FinalizacaoRecusada_NaoGravaNenhumaAlteracaoNoBanco` (`PersistenciaIntegracaoTests.cs`).
