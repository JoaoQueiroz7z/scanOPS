# scanOPS — Architecture Decision Records (ADR)

> Registro das decisões de arquitetura e **por que** foram tomadas. Ao mudar uma decisão, **não apague**: crie um novo ADR e marque o antigo como `Substituído`.
> Veja também: [`ARCHITECTURE.md`](ARCHITECTURE.md) e [`ROADMAP.md`](ROADMAP.md).
>
> Revisado em 15/11/2026: cada ADR ganhou uma linha **Implementação** dizendo o que existe no código.

**Status possíveis:** `Proposto` · `Aceito` · `Substituído` · `Rejeitado`

## Índice

| ADR | Título | Status | Implementado? |
|---|---|---|---|
| 001 | Back-end em C# / .NET 10 | Aceito | Sim |
| 002 | Front-end React + Vite separado do back-end | Aceito | Sim |
| 003 | Comunicação via API HTTP/JSON com DTOs | Aceito | Sim (DTOs desde `f0a26f1`) |
| 004 | Dupla métrica: peso + imagem | Aceito | Parcial (só peso no back-end) |
| 005 | Organização do código por domínio (pastas e namespaces) | Aceito | Sim |
| 006 | Hardware atrás de interfaces, com implementações simuladas | Aceito | Sim (balança); câmera não usada |
| 007 | Banco de dados e padrão Repository | Aceito | Sim (SQLite + EF Core) |
| 008 | Autenticação e perfis de usuário | Proposto | Parcial (login com hash; rotas abertas) |
| 009 | Documentação como contexto para desenvolvimento com IA | Aceito | Sim |
| 010 | Regra de negócio × falha excepcional, com tratamento central | Aceito | Sim |
| 011 | Regras no domínio e `ItemProcedimento` dentro do agregado | Aceito | Sim |
| 012 | RN7 (sala ocupada) em serviço de domínio, com índice único parcial | Aceito | Sim |
| 013 | Histórico com "foto" do instrumento no item | Aceito | Sim |

---

## ADR-001 — Back-end em C# / .NET 10

- **Status:** Aceito
- **Contexto:** O trabalho de POO exige uma linguagem orientada a objetos e o sistema precisa integrar com hardware (balança e câmera).
- **Decisão:** Usar C# com .NET 10 (API web) para as regras de negócio e a integração com o hardware.
- **Consequências:**
  - (+) Linguagem fortemente tipada, com bom suporte a interfaces, herança e generics, o que facilita demonstrar os pilares de POO.
  - (+) Ecossistema único para API, serviços e acesso a dispositivos.
  - (−) A equipe precisa conhecer o .NET; o front-end exige uma segunda tecnologia.
- **Implementação:** `scanOPS.csproj` (Sdk.Web, `net10.0`), minimal API em `Program.cs`.

## ADR-002 — Front-end React + Vite separado do back-end

- **Status:** Aceito
- **Contexto:** O professor indicou React com Vite para o front-end. Há mockup de Login pronto.
- **Decisão:** Manter o front em `frontend/` como aplicação independente, sem regra de negócio.
- **Consequências:**
  - (+) Front e back evoluem e rodam separados (`localhost:5173` e `localhost:5000`).
  - (+) Regras de negócio ficam só no back-end e são testáveis sem interface.
  - (−) Exige configurar CORS em desenvolvimento.
- **Implementação:** `frontend/`; CORS para `localhost:5173` e `5174` em `Program.cs`.

## ADR-003 — Comunicação via API HTTP/JSON com DTOs

- **Status:** Aceito
- **Contexto:** O front precisa operar o fluxo de contagem sem acesso direto ao hardware.
- **Decisão:** Toda comunicação passa por uma API HTTP com JSON. O back-end recebe e devolve **DTOs**, não entidades de domínio.
- **Consequências:**
  - (+) Contrato claro entre as camadas.
  - (+) O domínio fica protegido de mudanças na interface.
  - (−) Exige mapeamento entidade ↔ DTO.
- **Implementação:** até o commit `f0a26f1` a API **expunha as entidades** (`Instrumento`, `Paciente`), contrariando esta decisão; isso causava dois defeitos de integração (tipo de instrumento como número; data vazia recusada). Desde então, `Api/Dtos/` define `...Requisicao`/`...Resposta` para todas as rotas. Testes: `tests/ScanOPS.Tests/Api/ContratoApiTests.cs`.

## ADR-004 — Dupla métrica: peso + imagem

- **Status:** Aceito
- **Contexto:** Uma única métrica pode falhar (peça parecida na câmera ou peso próximo entre peças diferentes).
- **Decisão:** Cada item é conferido por **peso (balança)** e **reconhecimento por imagem (câmera)** ao mesmo tempo; qualquer divergência sinaliza o item.
- **Consequências:**
  - (+) Maior segurança contra falsos "OK".
  - (−) Mais complexidade e mais pontos de falha (dois dispositivos).
  - (−) Precisa de **tolerância de peso** definida.
- **Implementação (parcial):** a conferência de peso está no domínio (RN4, tolerância de ±2 g em `ItemProcedimento.ToleranciaPesoPadrao`). A câmera tem interface e simulação, mas **nenhuma rota a usa**: o campo `IdentificadoPorCamera` é informado pelo cliente e não entra no cálculo de divergência.

## ADR-005 — Organização do código por domínio

- **Status:** Aceito
- **Contexto:** O projeto cresce com várias classes; é preciso saber onde cada coisa fica.
- **Decisão:** Uma pasta por área (`Cirurgias`, `Contagem`, `Instrumentos`, `Pessoas`, `Hardware`, `Servicos`, `Relatorios`), com namespaces `ScanOPS.<Pasta>`. Nomes em português, como no repositório.
- **Consequências:**
  - (+) Fácil localizar código e delimitar tarefas.
  - (−) É preciso padronizar maiúsculas/minúsculas das pastas.
- **Implementação:** pastas acrescentadas depois: `Api/` (tratamento de erros e DTOs), `Excecoes/` e `Persistencia/`. Exceção ao padrão de namespace: `Contagem/` usa `ScanOPS.Contagens`. A pasta de documentação foi padronizada como `docs/` (minúscula) e o README foi para a raiz.

## ADR-006 — Hardware atrás de interfaces, com implementações simuladas

- **Status:** Aceito
- **Contexto:** Nem todos os integrantes têm balança/câmera; o hardware real é a parte mais arriscada.
- **Decisão:** Definir `IBalanca` e `ICamera`. O código que usa hardware depende só das interfaces. Implementações simuladas para desenvolvimento e testes; as reais entram depois. A escolha é feita por injeção de dependência.
- **Consequências:**
  - (+) Aplica polimorfismo, abstração e inversão de dependência.
  - (+) Toda a lógica pode ser testada sem equipamento.
  - (−) A integração real pode revelar diferenças que a simulação não cobre.
- **Implementação:** `IBalanca` com `BalancaSimulada` e `BalancaComRuido`, escolhida por `appsettings.json` → `Balanca:Tipo` (`Hardware/ConfiguracaoDeHardware.cs`) e usada por `ServicoDePesagem` na rota `/pesar`. `ICamera`/`CameraSimulada` existem, mas não são registradas nem usadas. Comparação com a alternativa (classe concreta) em [`av2/analise-modelo.md`](av2/analise-modelo.md).

## ADR-007 — Banco de dados e padrão Repository

- **Status:** Aceito (era Proposto)
- **Contexto:** Os dados ficavam em listas na memória e se perdiam a cada reinício da API.
- **Decisão:** **SQLite** com **Entity Framework Core 10**. O agregado `Procedimento` é acessado por `IRepositorioDeProcedimentos`, que sempre carrega o procedimento completo (itens, instrumentos e pessoas). Cadastros simples (instrumentos, pacientes, usuários) usam o `ScanOpsDbContext` direto nas rotas. O banco é criado por `EnsureCreated` na inicialização, com dados de demonstração.
- **Alternativas consideradas:** repositório genérico `IRepositorio<T>` para todas as classes (descartado: a única entidade com regras e composição é o `Procedimento`; para os cadastros seria uma camada sem comportamento); SQL Server/PostgreSQL (exigiriam instalar servidor; SQLite é um arquivo).
- **Consequências:**
  - (+) Nenhuma instalação: o banco é criado sozinho; testes usam um arquivo temporário por teste.
  - (−) Sem migrações: após mudar o modelo, é preciso apagar `scanops.db`.
- **Implementação:** `Persistencia/`; testes em `tests/ScanOPS.Tests/Api/PersistenciaIntegracaoTests.cs`.

## ADR-008 — Autenticação e perfis de usuário

- **Status:** Proposto
- **Contexto:** Há tela de Login e perfis (Cirurgião, Instrumentista, Administrador).
- **Decisão (proposta):** Autenticação por e-mail e senha com **hash**; o perfil define as ações permitidas. Conta de demonstração apenas em desenvolvimento.
- **Consequências:** (+) segurança mínima adequada a dados de saúde; (−) exige definir a matriz de permissões por perfil.
- **Implementação (parcial):** `Usuario.Autenticar` com hash SHA-256 e rota `/api/login`. **Não existe** sessão nem token: as demais rotas não exigem usuário autenticado e não há verificação de perfil (por exemplo, qualquer usuário pode ser registrado como cirurgião de um procedimento).

## ADR-009 — Documentação como contexto para desenvolvimento com IA

- **Status:** Aceito
- **Contexto:** O professor orienta documentar antes de codar com IA.
- **Decisão:** Manter `ARCHITECTURE.md`, `ADR.md` e `ROADMAP.md` em `docs/`. Pedidos de código à IA incluem esses arquivos e o bloco de regras da seção 10 do ARCHITECTURE.
- **Consequências:**
  - (+) Código gerado consistente com a arquitetura.
  - (−) Os documentos precisam ser mantidos atualizados: estes três ficaram desatualizados durante a implementação e foram revisados em 15/11/2026.
- **Implementação:** documentos em `docs/`; documentação da AV2 em `docs/av2/`.

## ADR-010 — Regra de negócio × falha excepcional, com tratamento central

- **Status:** Aceito
- **Contexto:** As rotas capturavam `InvalidOperationException` uma a uma, com `try/catch` repetido, e não havia distinção entre "o usuário pediu algo proibido" e "um equipamento falhou".
- **Decisão:** `RegraDeNegocioException` (`Excecoes/`) para violações de regra → HTTP 400 com a mensagem do domínio; `FalhaDeHardwareException` (`Hardware/`) para falhas de equipamento → HTTP 503 com mensagem genérica. Um único `IExceptionHandler` (`Api/TratadorDeExcecoes.cs`) decide código e mensagem e registra o log; as rotas não usam `try/catch`.
- **Alternativas:** `try/catch` em cada rota (duplicação e risco de esquecer); devolver códigos de erro em vez de exceções (o domínio teria de retornar resultados que toda rota precisaria checar).
- **Consequências:** (+) nenhum detalhe interno chega ao usuário; (+) comportamento uniforme; (−) o fluxo de erro fica menos visível na leitura de cada rota.
- **Implementação:** commit `09e70af`. Detalhes e testes em [`av2/tratamento-de-falhas.md`](av2/tratamento-de-falhas.md).

## ADR-011 — Regras no domínio e `ItemProcedimento` dentro do agregado

- **Status:** Aceito
- **Contexto:** O planejamento inicial previa regras em serviços (`CirurgiaService`, `ContagemService` etc.) e o código original deixava as rotas alterarem os itens diretamente. Isso permitiu um defeito real: procedimento finalizado aceitava novas contagens.
- **Decisão:** As regras ficam nos métodos do domínio. `Procedimento` é a raiz do agregado: só ele cria e altera `ItemProcedimento` (construtor e métodos `internal`, lista `IReadOnlyList`) e só ele muda o próprio status. `Finalizar` valida tudo antes de alterar qualquer coisa. Serviços existem apenas para coordenar o que não pertence ao domínio (`ServicoDePesagem`, que liga a balança ao procedimento).
- **Consequências:** (+) a invariante "registro fechado não muda" tem um único guardião; (−) é preciso carregar o procedimento inteiro para alterar um item.
- **Implementação:** commits `eb9a8ed` (correção) e `4495c04` (encapsulamento). Comparação com a alternativa em [`av2/analise-modelo.md`](av2/analise-modelo.md); depuração do defeito em [`av2/registro-depuracao.md`](av2/registro-depuracao.md).

## ADR-012 — RN7 (sala ocupada) em serviço de domínio, com índice único parcial

- **Status:** Aceito
- **Contexto:** Com o controle de salas, pode haver vários procedimentos em andamento ao mesmo tempo, mas nunca dois na mesma sala. A regra depende de outros procedimentos, então um `Procedimento` sozinho não consegue garanti-la.
- **Decisão:** `Servicos/ServicoDeInicioDeProcedimento` consulta o repositório (`ObterEmAndamentoNaSala`) antes de chamar `Procedimento.Iniciar()`. A sala é gravada padronizada (`Cirurgias/SalaCirurgica`, "sala 02" → "Sala 2"). Proteção extra: índice único parcial no SQLite (`Sala` onde `Status = EmAndamento`); a violação é convertida em `RegraDeNegocioException` pelo repositório. A operação passou a identificar o procedimento pelo id (`/api/procedimentos/{id}/...`); as rotas `/api/procedimento-atual` ficaram por compatibilidade.
- **Alternativas:** checar dentro do `Procedimento` (o domínio dependeria do repositório); só o índice do banco (a mensagem ao usuário seria genérica e o erro apareceria tarde).
- **Consequências:** (+) cada classe cuida do que conhece; (+) corrida entre dois pedidos barrada pelo banco; (−) quem chamar `Procedimento.Iniciar()` direto contorna o serviço (o índice ainda barra ao gravar); (−) é preciso apagar o `scanops.db` antigo para o índice ser criado.
- **Implementação:** detalhes em [`av2/analise-modelo.md`](av2/analise-modelo.md) (decisão 4); testes em `tests/ScanOPS.Tests/Api/SalasRn7Tests.cs`.

## ADR-013 — Histórico com "foto" do instrumento no item

- **Status:** Aceito
- **Contexto:** O histórico de cirurgias precisa mostrar o instrumento e o peso de referência que valiam na cirurgia, mas o cadastro de instrumentos pode ser renomeado ou ter o peso corrigido.
- **Decisão:** Ao entrar no procedimento, o `ItemProcedimento` guarda `NomeInstrumento` e `PesoReferencia`; a RN4 confere com esse peso. Paciente e cirurgião continuam lidos do cadastro (correções de nome devem valer em tudo).
- **Alternativas:** ler sempre o cadastro atual (o histórico mudaria retroativamente).
- **Consequências:** (+) registro fiel ao que aconteceu; (−) duas colunas a mais por item; correções de cadastro valem só para procedimentos novos.
- **Implementação:** `Contagem/ItemProcedimento.cs`; comparação em [`av2/analise-modelo.md`](av2/analise-modelo.md) (decisão 3).

---

## Modelo para novos ADRs

```markdown
## ADR-0XX — Título

- **Status:** Proposto
- **Contexto:** (problema e restrições)
- **Decisão:** (o que será feito)
- **Alternativas:** (o que foi considerado)
- **Consequências:** (+) ... (−) ...
- **Implementação:** (onde está no código; commit)
```
