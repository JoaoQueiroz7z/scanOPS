# scanOPS — Architecture Decision Records (ADR)

> Registro das decisões de arquitetura e **por que** foram tomadas. Ao mudar uma decisão, **não apague**: crie um novo ADR e marque o antigo como `Substituído`.
> Veja também: [`ARCHITECTURE.md`](ARCHITECTURE.md) e [`ROADMAP.md`](ROADMAP.md).

**Status possíveis:** `Proposto` · `Aceito` · `Substituído` · `Rejeitado`

## Índice

| ADR | Título | Status |
|---|---|---|
| 001 | Back-end em C# / .NET 10 | Aceito |
| 002 | Front-end React + Vite separado do back-end | Aceito |
| 003 | Comunicação via API HTTP/JSON | Aceito |
| 004 | Dupla métrica: peso + imagem | Aceito |
| 005 | Organização do código por domínio (pastas e namespaces) | Aceito |
| 006 | Hardware atrás de interfaces, com implementações simuladas | Aceito |
| 007 | Banco de dados e padrão Repository | Proposto |
| 008 | Autenticação e perfis de usuário | Proposto |
| 009 | Documentação como contexto para desenvolvimento com IA | Aceito |

---

## ADR-001 — Back-end em C# / .NET 10

- **Status:** Aceito
- **Contexto:** O trabalho de POO exige uma linguagem orientada a objetos e o sistema precisa integrar com hardware (balança e câmera).
- **Decisão:** Usar C# com .NET 10 (API web) para as regras de negócio e a integração com o hardware.
- **Consequências:**
  - (+) Linguagem fortemente tipada, com bom suporte a interfaces, herança e generics, o que facilita demonstrar os pilares de POO.
  - (+) Ecossistema único para API, serviços e acesso a dispositivos.
  - (−) A equipe precisa conhecer o .NET; o front-end exige uma segunda tecnologia.

## ADR-002 — Front-end React + Vite separado do back-end

- **Status:** Aceito
- **Contexto:** O professor indicou React com Vite para o front-end. Há mockup de Login pronto.
- **Decisão:** Manter o front em `frontend/` como aplicação independente, sem regra de negócio.
- **Consequências:**
  - (+) Front e back evoluem e rodam separados (`localhost:5173` e `localhost:5000`).
  - (+) Regras de negócio ficam só no back-end e são testáveis sem interface.
  - (−) Exige configurar CORS em desenvolvimento.

## ADR-003 — Comunicação via API HTTP/JSON

- **Status:** Aceito
- **Contexto:** O front precisa operar o fluxo de contagem sem acesso direto ao hardware.
- **Decisão:** Toda comunicação passa por uma API HTTP com JSON. O back-end devolve **DTOs**, não entidades de domínio.
- **Consequências:**
  - (+) Contrato claro entre as camadas.
  - (+) O domínio fica protegido de mudanças na interface.
  - (−) Exige mapeamento entidade ↔ DTO.

## ADR-004 — Dupla métrica: peso + imagem

- **Status:** Aceito
- **Contexto:** Uma única métrica pode falhar (peça parecida na câmera ou peso próximo entre peças diferentes).
- **Decisão:** Cada item é conferido por **peso (balança)** e **reconhecimento por imagem (câmera)** ao mesmo tempo; qualquer divergência sinaliza o item.
- **Consequências:**
  - (+) Maior segurança contra falsos "OK".
  - (−) Mais complexidade e mais pontos de falha (dois dispositivos).
  - (−) Precisa de **tolerância de peso** definida (ver ARCHITECTURE, RN4).

## ADR-005 — Organização do código por domínio

- **Status:** Aceito
- **Contexto:** O projeto cresce com várias classes; é preciso saber onde cada coisa fica.
- **Decisão:** Uma pasta por área do domínio (`Cirurgias`, `Contagem`, `Instrumentos`, `Pessoas`, `Hardware`, `Servicos`, `Relatorios`), com namespaces `ScanOPS.<Pasta>`. Nomes em português, como no repositório.
- **Consequências:**
  - (+) Fácil localizar código e delimitar tarefas para a IA.
  - (−) É preciso padronizar maiúsculas/minúsculas das pastas (README × GitHub).

## ADR-006 — Hardware atrás de interfaces, com implementações simuladas

- **Status:** Aceito
- **Contexto:** Nem todos os integrantes têm balança/câmera; o hardware real é a parte mais arriscada.
- **Decisão:** Definir `IBalanca` e `ICamera`. Serviços dependem só das interfaces. Criar `BalancaSimulada` e `CameraSimulada` para desenvolvimento e testes; as reais entram depois. A escolha é feita por injeção de dependência em `Program.cs`.
- **Consequências:**
  - (+) Aplica polimorfismo, abstração e inversão de dependência.
  - (+) Toda a lógica pode ser testada sem equipamento.
  - (−) A integração real pode revelar diferenças que a simulação não cobre.

## ADR-007 — Banco de dados e padrão Repository

- **Status:** Proposto
- **Contexto:** O README lista persistência como pendente, e o banco ainda não foi escolhido.
- **Decisão (proposta):** Acessar dados por interfaces de repositório (`IRepositorio<T>`), sem acoplar os serviços a um banco específico. Escolher o SGBD (ex.: SQLite para o trabalho acadêmico) e o acesso a dados (ex.: Entity Framework Core) em reunião da equipe.
- **Alternativas:** repositório em memória na primeira fase; SQLite; SQL Server/PostgreSQL.
- **Consequências:** permite começar sem banco e trocar a implementação depois. **Pendente:** registrar a escolha final e mudar o status para Aceito.

## ADR-008 — Autenticação e perfis de usuário

- **Status:** Proposto
- **Contexto:** Há tela de Login e perfis nos diagramas (Cirurgião, Instrumentista, Administrador). O README mostra uma conta de demonstração.
- **Decisão (proposta):** Autenticação por e-mail e senha com **hash** de senha; perfil define as ações permitidas. Conta de demonstração apenas em ambiente de desenvolvimento.
- **Consequências:** (+) segurança mínima adequada a dados de saúde; (−) exige definir a matriz de permissões por perfil. **Pendente:** definir mecanismo de sessão/token.

## ADR-009 — Documentação como contexto para desenvolvimento com IA

- **Status:** Aceito
- **Contexto:** O professor orienta documentar tudo antes de codar com IA.
- **Decisão:** Manter `ARCHITECTURE.md`, `ADR.md` e `ROADMAP.md` na raiz. Todo pedido de código à IA inclui esses arquivos como contexto e o bloco de regras da seção 11 do ARCHITECTURE.
- **Consequências:**
  - (+) Código gerado consistente com a arquitetura.
  - (−) Os documentos precisam ser mantidos atualizados a cada mudança relevante.

---

## Modelo para novos ADRs

```markdown
## ADR-0XX — Título

- **Status:** Proposto
- **Contexto:** (problema e restrições)
- **Decisão:** (o que será feito)
- **Alternativas:** (o que foi considerado)
- **Consequências:** (+) ... (−) ...
```
