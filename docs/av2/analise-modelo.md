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

## Decisão complementar — estado nomeado (enum) × booleanos

`Procedimento` usa `StatusProcedimento` (`Planejado`, `EmAndamento`, `Finalizado`, `Cancelado`) em
vez de flags como `Iniciado` e `Finalizado`. Com dois booleanos haveria uma combinação impossível
(`Finalizado = true` e `Iniciado = false`) que o código teria de impedir em todo lugar; com o enum,
cada estado é um valor único e as transições ficam explícitas nos métodos `Iniciar`, `Finalizar` e
`Cancelar` (ver diagrama de estados em [especificacao.md](especificacao.md)).
