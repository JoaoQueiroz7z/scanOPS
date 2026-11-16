# Revisão de código — alteração `eb9a8ed`

## 1. Alteração selecionada e ponto de comparação (C01)

| | |
|---|---|
| Commit revisado | `eb9a8ed` — *fix: impede contagem em procedimento finalizado e quantidade inválida* |
| Base de comparação | `7782869` — commit imediatamente anterior, que adicionou os testes que **falham** reproduzindo o defeito |
| Comando do diff | `git diff 7782869 eb9a8ed` (ou `git show eb9a8ed`) |
| Branch / PR | `fix/validacao-contagem`, integrada à `main` pelo PR #1 (`65892d5`) |

É uma alteração **funcional**, de comportamento do domínio: muda quais contagens o sistema aceita.

## 2. Requisito que a alteração implementa (C02)

**RN5** — contagens só são aceitas com o procedimento `EmAndamento` e com quantidade válida
(entrada > 0; saída ≥ 0). Ver [especificacao.md](especificacao.md).

Comportamento antes (observado com a API rodando, `docs/av2/evidencias/reproducao-antes-da-correcao.txt`):
depois de `Finalizado`, `POST .../entrada` com `3` e com `-5` devolveram **200**, e o item ficou com
`quantidadeInicial: -2`. Comportamento depois (`reproducao-depois-da-correcao.txt`): **400** com
mensagem, contagem intacta. Inspeção orientada pelo requisito:

| Pergunta do requisito | Onde o diff responde |
|---|---|
| Contagem é recusada fora de `EmAndamento`? | `GarantirContagemPermitida()` chamado no início de `RegistrarEntrada` e `RegistrarSaida` |
| Entrada ≤ 0 é recusada? | `if (quantidade <= 0) throw ...` em `RegistrarEntrada` |
| Saída 0 continua aceita (tela "simular falta" envia 0)? | `if (quantidade < 0)` — só negativos são recusados |
| A API devolve erro ao usuário em vez de 200? | rotas `/entrada` e `/saida` capturam a exceção e devolvem 400 |

## 3. Inventário dos arquivos modificados (C03)

`git show --stat eb9a8ed`: 2 arquivos, +61 / −12.

| Arquivo | Finalidade da mudança | Dentro do escopo? |
|---|---|---|
| `Cirurgias/Procedimento.cs` (+35) | Novos métodos públicos `RegistrarEntrada(itemId, qtd)` e `RegistrarSaida(itemId, qtd, peso, camera)`; auxiliares privados `GarantirContagemPermitida()` (checa o status) e `ObterItem()` (lança `KeyNotFoundException` se o item não existe) | Sim — é onde a regra passa a morar |
| `Program.cs` (+26 / −12) | Rotas `/entrada` e `/saida` deixam de buscar o item e alterá-lo diretamente; passam a chamar os métodos do `Procedimento` e traduzem `KeyNotFoundException` → 404 e `InvalidOperationException` → 400 | Sim — sem isso a regra não seria usada pela API |

**Mudanças fora do escopo:** nenhuma neste commit. Não há alteração de front-end, de formatação
ou de outras rotas. O ajuste necessário para testar a API (`public partial class Program { }`)
ficou no commit anterior (`7782869`), junto dos testes, e não se mistura com a correção.

## 4. Impacto da alteração (C04)

**Impacto local**
- `Procedimento` ganha responsabilidade sobre a contagem; `ItemProcedimento` não muda.
- Rotas `/entrada` e `/saida`: o **contrato HTTP muda** — casos que antes davam 200 agora dão 400 (contagem após finalizar, entrada ≤ 0, saída < 0) e item inexistente continua 404.

**Impacto transitivo**
- Front-end (da época deste commit; essas telas foram substituídas depois pelo front novo, que também aceita saída 0): `ContagemInicial.jsx` envia entrada `1`; `ContagemFinal.jsx` envia saída igual à
  inicial ou `inicial - 1` (pode ser 0). Os dois continuam aceitos → as telas não quebram.
- Demais rotas (instrumentos, pacientes, login, finalizar): não tocadas.

**Contratos que merecem atenção**
- O tipo `InvalidOperationException` passou a significar "regra violada" nas rotas. É genérico
  demais: outro erro do .NET do mesmo tipo também viraria 400 com mensagem interna.
- `ItemProcedimento.RegistrarEntrada/RegistrarSaida` **continuam públicos**: quem chamar o item
  direto ainda contorna a regra.

**Verificação** (`docs/av2/evidencias/revisao-verificacao.txt`, executado a partir das duas versões):

| Versão | Resultado de `dotnet test` |
|---|---|
| `7782869` (antes) | 2 falhas: `RegistrarEntrada_DepoisDeFinalizado_...` e `RegistrarEntrada_QuantidadeNegativa_...` — *Expected: BadRequest, Actual: OK* |
| `eb9a8ed` (depois) | 4 de 4 aprovados |

## 5. Avaliação quanto a propriedades de POO (C05)

| Propriedade | Avaliação |
|---|---|
| **Responsabilidade / coesão** | Melhorou. A regra "não contar fora de andamento" foi para o objeto que conhece o status (`Procedimento`). Antes, a regra não existia em lugar nenhum, e a rota fazia o papel do domínio. |
| **Encapsulamento** | Melhorou, mas **incompleto**. `Procedimento` passa a ser a porta da contagem, porém os métodos do item seguem públicos e `PesoMedido`/`IdentificadoPorCamera` seguem com `set` público. |
| **Acoplamento** | Diminuiu. A rota não navega mais em `Itens` nem conhece os detalhes do item; depende só de `Procedimento.RegistrarEntrada/RegistrarSaida`. |
| **Duplicação** | Piorou levemente: os dois blocos `try/catch` nas rotas são idênticos (sinal de que o tratamento deveria ser central). |

## 6. Parecer técnico (C06)

**Fatos:** o diff implementa a RN5 nos dois pontos de entrada da contagem; os testes que reproduziam
o defeito passaram de falha para aprovação; a reprodução manual passou de 200 para 400; as telas
existentes continuam compatíveis.

**Julgamento:** a correção é a menor alteração coerente para eliminar o defeito e está no lugar
certo. As ressalvas (encapsulamento incompleto, exceção genérica e `try/catch` duplicado) não
invalidam a correção, mas precisavam de tratamento.

**Decisão: ACEITAR**, com as ressalvas registradas como trabalho seguinte — que foi feito e pode
ser verificado no histórico:

| Ressalva | Tratada em |
|---|---|
| Exceção genérica e `try/catch` duplicado nas rotas | `09e70af` — `RegraDeNegocioException` + tratamento central `Api/TratadorDeExcecoes.cs`; rotas sem `try/catch` |
| Métodos do item ainda públicos | `4495c04` — construtor e métodos de alteração de `ItemProcedimento` passam a `internal`; testes de auditoria de encapsulamento |
| Validações espalhadas no `Finalizar` | `4495c04` — `Finalizar` valida antes de alterar (evidência em `docs/av2/evidencias/invariante-finalizar.txt`) |
