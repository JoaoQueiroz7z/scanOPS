# Análise do modelo — decisões de modelagem

Cada decisão compara alternativas pelos mesmos critérios e justifica a escolha com elementos
concretos do código e dos testes. Modelo de referência: [modelo-classes.md](modelo-classes.md).

---

## Decisão 1 — Balança atrás de uma interface (`IBalanca`) × classe concreta

**Contexto.** A conferência de peso (RN4) depende de uma balança. Hoje não há equipamento real;
existem simulações, e no futuro haverá um driver de balança de verdade.

**Alternativa A — classe concreta usada diretamente.** A rota ou o serviço faria
`var balanca = new BalancaSimulada();` e chamaria `balanca.Pesar()`.

**Alternativa B (escolhida) — contrato `IBalanca` com implementações intercambiáveis.**
`ServicoDePesagem` recebe um `IBalanca` no construtor; qual implementação usar é decidido por
configuração em `Hardware/ConfiguracaoDeHardware.cs` (`Balanca:Tipo` no `appsettings.json`).

| Critério | A — classe concreta | B — interface |
|---|---|---|
| Acoplamento | Serviço depende de `BalancaSimulada`; trocar de balança exige mudar o serviço e a rota | Serviço depende só de 3 métodos (`Pesar`, `Calibrar`, `VerificarConexao`) |
| Variação real | Para ter `BalancaComRuido` seria preciso `if (tipo == ...)` espalhado pelo código | Cada balança implementa o próprio comportamento; nenhum `if` por tipo fora da configuração |
| Testabilidade | Não há como simular balança desligada sem mudar `BalancaSimulada` | Dublês de teste (`BalancaDesconectada`, `BalancaComErroDeLeitura`) entram pela mesma porta |
| Tratamento de falha | Erro do equipamento apareceria como exceção qualquer | `ServicoDePesagem` converte qualquer falha em `FalhaDeHardwareException` → HTTP 503 |
| Custo | Mais simples de ler no início | Uma indireção a mais e registro na injeção de dependência |

**Risco considerado.** Uma interface com uma única implementação seria abstração especulativa.
No início do projeto era o caso (só existia `BalancaSimulada`). A decisão passou a se justificar
quando surgiu **variação real** de comportamento: `BalancaComRuido` (ruído e deriva, `Calibrar`
com outro efeito) e os dublês de teste.

**Evidências.** `Servicos/ServicoDePesagem.cs`; `Hardware/BalancaSimulada.cs`, `Hardware/BalancaComRuido.cs`;
testes `MesmoServico_ComQualquerBalanca_GravaPesoQueRespeitaRN4`, `F3_Servico_BalancaDesconectada_LancaFalhaDeHardwareENaoAlteraPeso`,
`F3_Api_ErroNaBalanca_Retorna503ComMensagemGenericaSemDetalhesInternos`; evidência de execução em
`docs/av2/evidencias/polimorfismo-balanca.txt`.

**Conclusão.** B, porque há duas implementações com comportamentos diferentes em uso e a
falha de hardware precisa ser isolada e testada sem equipamento.

---

## Decisão 2 — `ItemProcedimento` dentro do agregado `Procedimento` × entidade independente

**Contexto.** A contagem é registrada por item (um tipo de instrumento), mas as regras dependem
do estado do procedimento inteiro: não se conta em procedimento finalizado (RN5) e a finalização
depende de todos os itens (RN3).

**Alternativa A — item como entidade independente.** O item teria setters/métodos públicos e
poderia ser buscado e alterado diretamente (por exemplo, `item.RegistrarEntrada(qtd)` chamado pela
rota, ou um repositório e rotas próprios para itens).

**Alternativa B (escolhida) — item como parte do agregado.** Só o `Procedimento` cria e altera
itens: `AdicionarInstrumento`, `RegistrarEntrada(itemId, ...)`, `RegistrarSaida(itemId, ...)`,
`RegistrarPeso(itemId, ...)`. O construtor e os métodos de alteração do item são `internal`, a lista
é exposta como `IReadOnlyList`, e o repositório carrega e grava o procedimento inteiro.

| Critério | A — entidade independente | B — dentro do agregado |
|---|---|---|
| Proteção da invariante | Cada rota precisaria lembrar de checar o status do procedimento antes de mexer no item | A checagem fica num só lugar (`GarantirContagemPermitida`) e não há caminho que a contorne |
| Consistência | Item pode ser alterado depois do fechamento do registro cirúrgico | Procedimento finalizado não aceita alteração; finalização recusada não deixa estado parcial |
| Coesão | Regra de status fica fora de quem conhece o status | Regra fica junto do dado que ela protege |
| Custo | Acesso direto e leve ao item | Sempre carrega o procedimento com os itens (poucos itens por cirurgia: custo baixo) |

**O defeito real que motivou a decisão.** Na versão original, as rotas faziam
`procedimentoAtivo.Itens.FirstOrDefault(...)` e chamavam `item.RegistrarEntrada(qtd)` direto —
a Alternativa A. Resultado: depois de `Finalizado`, a API aceitou `+3` e `-5` e o item ficou com
`quantidadeInicial: -2` e status `Conferido`
(`docs/av2/evidencias/reproducao-antes-da-correcao.txt`, análise em [registro-depuracao.md](registro-depuracao.md)).
A correção (`eb9a8ed`) e o encapsulamento (`4495c04`) implementaram a Alternativa B.

**Evidências.** `Cirurgias/Procedimento.cs`, `Contagem/ItemProcedimento.cs`,
`Persistencia/ScanOpsDbContext.cs` (cascata) e `Persistencia/RepositorioDeProcedimentos.cs`;
testes `ProcedimentoFinalizado_RecusaTodaAlteracaoEPermaneceIntacto`,
`ItemProcedimento_MetodosDeAlteracaoEConstrutorNaoSaoPublicos`, `FinalizacaoRecusada_NaoGravaNenhumaAlteracaoNoBanco`.

**Conclusão.** B, porque a regra mais importante do sistema (registro fechado não muda) depende
do estado do procedimento, e a alternativa A já se mostrou incapaz de protegê-la.

---

## Decisão 3 — Histórico com "foto" do instrumento × consulta ao cadastro atual

**Contexto.** O histórico de cirurgias precisa mostrar, para cada item, o instrumento usado e o peso
de referência que valia na conferência (RN4). Mas o cadastro de instrumentos muda: um instrumento
pode ser renomeado ("Pinça Kelly" → "Pinça Kelly Curva") ou ter o peso de referência corrigido.

**Alternativa A — consultar o dado atual.** O item guarda só a ligação com o `Instrumento`; nome e peso
são lidos do cadastro na hora de mostrar (era assim antes: `Instrumento.Nome`, `Instrumento.PesoReferencia`).

**Alternativa B (escolhida) — guardar uma "foto".** Quando o instrumento entra no procedimento
(`Procedimento.AdicionarInstrumento` → construtor de `ItemProcedimento`), o item copia
`NomeInstrumento` e `PesoReferencia`. A ligação com o `Instrumento` continua existindo (para saber
qual instrumento foi usado e impedir sua exclusão), mas o que se mostra e o que a RN4 confere é a foto.

| Critério | A — dado atual | B — foto no item |
|---|---|---|
| Fidelidade do histórico | Um registro cirúrgico antigo passa a mostrar um nome que não existia na época | Mostra exatamente o que foi usado e conferido |
| Conferência de peso (RN4) | Se o peso cadastrado for corrigido no meio da cirurgia, um item que conferia passa a divergir (ou o contrário) | O critério fica fixo desde o planejamento |
| Auditoria | Não dá para reconstituir por que a cirurgia foi aceita | A justificativa e os pesos do momento ficam juntos |
| Custo | Nenhum dado repetido | Duas colunas a mais por item (`NomeInstrumento`, `PesoReferencia`) |
| Correções de cadastro | Aparecem em tudo, inclusive no passado | Valem só para procedimentos novos |

**Por que não fotografar também paciente e cirurgião.** Para pessoas, a alternativa A é a certa:
uma correção de nome (erro de digitação) deve aparecer em todos os registros, e o vínculo com a pessoa
é o que importa clinicamente. Para instrumentos, o que importa é a especificação do objeto usado,
que pode mudar no cadastro sem que o objeto da cirurgia mude.

**Evidências.** `Contagem/ItemProcedimento.cs` (`NomeInstrumento`, `PesoReferencia`, `PesoEsperado()`);
testes `Foto_InstrumentoRenomeadoEComPesoCorrigidoDepois_NaoMudaOItem` (API) e
`Foto_ItemGuardaNomeEPesoDoMomentoDoPlanejamento`, `Foto_RN4UsaOPesoDaFoto` (domínio).

**Conclusão.** B para instrumentos, porque o histórico é um registro cirúrgico e precisa refletir a
cirurgia como ela aconteceu; A para pessoas, porque correções de cadastro devem valer em todo lugar.

---

## Decisão 4 — RN7 num serviço de domínio × dentro do `Procedimento`

**Contexto.** RN7: só um procedimento `EmAndamento` por sala. Para saber se a sala está livre é preciso
olhar **outros** procedimentos.

**Alternativa A — dentro de `Procedimento.Iniciar()`.** O procedimento teria de receber a lista de todos os
outros procedimentos (ou o repositório) para conferir a sala. O domínio passaria a depender da
persistência e o método ficaria responsável por algo que não é dele.

**Alternativa B (escolhida) — serviço de domínio.** `Servicos/ServicoDeInicioDeProcedimento` consulta o
repositório (`ObterEmAndamentoNaSala`) e só então chama `Procedimento.Iniciar()`, que continua cuidando
só da RN1. Proteção extra: índice único parcial no banco (`Sala` onde `Status = EmAndamento`), cuja
violação o repositório converte em `RegraDeNegocioException`.

| Critério | A — no Procedimento | B — serviço + índice |
|---|---|---|
| Responsabilidade | O objeto passaria a cuidar de uma regra entre vários objetos | Cada um cuida do que conhece: o procedimento do próprio estado; o serviço da ocupação da sala |
| Dependências | Domínio dependeria do repositório | O domínio continua sem saber de banco; o serviço depende da interface `IRepositorioDeProcedimentos` |
| Concorrência | Dois pedidos simultâneos passariam pela checagem em memória | O índice do banco barra o segundo |
| Risco | — | A regra fica fora do agregado: quem chamar `Procedimento.Iniciar()` direto contorna a RN7 (só a rota usa o serviço; o índice cobre a gravação) |

**Evidências.** `Servicos/ServicoDeInicioDeProcedimento.cs`, `Persistencia/ScanOpsDbContext.cs` (índice),
`Persistencia/RepositorioDeProcedimentos.cs` (conversão do erro); testes em `tests/ScanOPS.Tests/Api/SalasRn7Tests.cs`.

---

## Decisão complementar — estado nomeado (enum) × booleanos

`Procedimento` usa `StatusProcedimento` (`Planejado`, `EmAndamento`, `Finalizado`, `Cancelado`) em
vez de flags como `Iniciado` e `Finalizado`. Com dois booleanos haveria uma combinação impossível
(`Finalizado = true` e `Iniciado = false`) que o código teria de impedir em todo lugar; com o enum,
cada estado é um valor único e as transições ficam explícitas nos métodos `Iniciar`, `Finalizar` e
`Cancelar` (ver diagrama de estados em [especificacao.md](especificacao.md)).
