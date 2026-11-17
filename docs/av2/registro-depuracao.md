# Registro de depuração — contagem aceita depois de finalizar

## 1. Defeito (E01)

**Sintoma:** depois que um procedimento é **finalizado**, a API continua aceitando novas contagens,
inclusive com quantidade negativa. O registro cirúrgico, que deveria estar fechado, é alterado.

**Tipo:** defeito real do código original (não foi introduzido para o trabalho). Encontrado na
leitura do código e confirmado por execução.

**Linha de base:** versão da `main` antes da correção. O defeito foi reproduzido no commit
`7782869` e corrigido em `eb9a8ed` (branch `fix/validacao-contagem`, PR #1).

## 2. Reprodução: condições, esperado e observado (E02)

**Condições:**
- API rodando localmente (`dotnet run --urls http://localhost:5077`), dados em memória, procedimento de demonstração `EmAndamento`.
- Sequência: `GET /api/procedimento-atual` → `POST /finalizar` (sem divergência) → `POST /itens/{id}/entrada` com `3` → `POST /itens/{id}/entrada` com `-5` → `GET` de novo.

| Passo | Esperado | Observado |
|---|---|---|
| Finalizar | 200, status `Finalizado` | 200, status `Finalizado` |
| Entrada de 3 após finalizar | **400**, contagem inalterada | **200** |
| Entrada de −5 | **400** | **200** |
| Estado final do item | `quantidadeInicial: 0` | `quantidadeInicial: -2`, `diferenca: -2`, mas `status: "Conferido"` |

Evidência: `docs/av2/evidencias/reproducao-antes-da-correcao.txt` (saída real dos comandos).

Reprodução automatizada: testes `RegistrarEntrada_DepoisDeFinalizado_RetornaBadRequestENaoAlteraContagem`
e `RegistrarEntrada_QuantidadeNegativa_RetornaBadRequest` (`tests/ScanOPS.Tests/Api/ContagemAposFinalizarTests.cs`),
commitados **falhando** em `7782869` (`docs/av2/evidencias/teste-falhando-antes-da-correcao.txt`).

## 3. Hipóteses e investigação (E03)

Reduzindo o espaço de busca: a resposta é 200, então a requisição chega à rota e não há exceção.
O defeito está no caminho rota → item.

| # | Hipótese | Como testar | Resultado |
|---|---|---|---|
| H1 | A rota não consulta o status do procedimento antes de alterar o item | Log temporário na rota `/entrada` imprimindo `procedimento.Status` e a quantidade antes/depois | **Confirmada**: `Status=Finalizado` e mesmo assim `QuantidadeInicial` foi de 0 → 3 → −2 |
| H2 | `Finalizar()` não muda o status de fato (o procedimento continuaria `EmAndamento`) | Mesmo log + resposta do `POST /finalizar` | **Descartada**: o log mostra `Status=Finalizado` no momento da entrada |
| H3 | `ItemProcedimento.RegistrarEntrada` não valida a quantidade | Leitura do método: `QuantidadeInicial += qtd;` sem condição | **Confirmada** (explica o −5), mas é consequência: o item não tem como saber o status do procedimento |

## 4. Técnica de depuração aplicada (E04)

**Técnica:** análise de logs com instrumentação temporária (rastreamento da execução).

Na versão com defeito (`7782869`), extraída numa pasta temporária fora do repositório, foram
inseridas duas linhas `Console.WriteLine("[DEPURACAO] ...")` na rota `/entrada`, antes e depois de
`item.RegistrarEntrada(...)`. O mesmo roteiro de reprodução foi executado e o log do servidor foi
capturado:

```
[DEPURACAO] POST /entrada recebido: quantidade=3, procedimento.Status=Finalizado, item.QuantidadeInicial(antes)=0
[DEPURACAO] RegistrarEntrada executado SEM checar status: item.QuantidadeInicial(depois)=3
[DEPURACAO] POST /entrada recebido: quantidade=-5, procedimento.Status=Finalizado, item.QuantidadeInicial(antes)=3
[DEPURACAO] RegistrarEntrada executado SEM checar status: item.QuantidadeInicial(depois)=-2
```

Evidência completa (trecho instrumentado, respostas HTTP e log): `docs/av2/evidencias/depuracao-log.txt`.
A instrumentação **não** foi commitada.

## 5. Causa raiz e correção (E05)

**Localização × causa.** O sintoma aparece em `ItemProcedimento.RegistrarEntrada` (onde o número
fica errado), mas a **causa** não está ali: o item não conhece o status do procedimento e não tem
como recusar. A causa raiz é de **responsabilidade**: a regra "registro fechado não muda" não
estava em lugar nenhum, porque as rotas alteravam o item diretamente, contornando o `Procedimento`,
que é o único objeto que conhece o status.

**Menor correção coerente** (`eb9a8ed`): criar `Procedimento.RegistrarEntrada(itemId, qtd)` e
`Procedimento.RegistrarSaida(...)`, que verificam o status (`GarantirContagemPermitida`) e a
quantidade antes de delegar ao item, e fazer as rotas usarem esses métodos. Análise do diff em
[revisao-codigo.md](revisao-codigo.md). Fechamento definitivo do desvio (métodos do item `internal`)
em `4495c04`.

## 6. Verificação antes × depois e não regressão (E06)

| | Antes (`7782869`) | Depois (`eb9a8ed`) |
|---|---|---|
| Entrada de 3 após finalizar | 200, contagem 0 → 3 | 400 `"Só é possível registrar contagem em procedimento em andamento."`, contagem 0 |
| Entrada de −5 | 200, contagem 3 → −2 | 400 |
| Testes de reprodução | 2 falham | 4 de 4 aprovados |

Evidências: `reproducao-antes-da-correcao.txt` × `reproducao-depois-da-correcao.txt`;
`teste-falhando-antes-da-correcao.txt` × `teste-passando-depois-da-correcao.txt`;
execução das duas versões em `revisao-verificacao.txt`.

**Não regressão:** os testes de reprodução continuam na suíte e rodam em todo `dotnet test`
(hoje, 148 testes). Foram acrescentados testes de domínio da mesma regra
(`ProcedimentoContagemTests.cs`) e, depois da persistência, a verificação no banco
(`ContagemAposFinalizar_ERecusadaEOBancoContinuaComOsValoresFinais`).

### Comparação com uma técnica alternativa: breakpoint no depurador do VS Code

| Critério | Log temporário (usado) | Breakpoint + inspeção de variáveis |
|---|---|---|
| Esforço para montar | Duas linhas de código e rodar o roteiro de novo | Configurar o depurador (`launch.json`) e anexar ao processo da API |
| Registro da evidência | O texto do log é a evidência, repetível e versionável | Exige captura de tela; não é repetível automaticamente |
| Vários pedidos seguidos | Mostra a sequência inteira (0 → 3 → −2) de uma vez | Para em cada pedido; é preciso continuar manualmente |
| Explorar o desconhecido | Só mostra o que foi escolhido para imprimir | Permite inspecionar qualquer variável e a pilha de chamadas no momento |
| Risco | Esquecer o log no código (mitigado: feito numa cópia fora do repositório) | Nenhum no código |

**Recomendação.** Para este defeito — sequência curta de pedidos HTTP e uma hipótese já formulada
sobre um único valor (o status) — o log foi mais adequado: confirmou a hipótese numa execução e
deixou evidência textual. O breakpoint seria a melhor escolha se a hipótese ainda não existisse e
fosse preciso explorar o estado do objeto passo a passo.
