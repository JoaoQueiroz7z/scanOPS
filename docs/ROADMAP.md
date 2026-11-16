# scanOPS — Roadmap

> Ordem de construção do sistema e situação de cada item.
> Veja também: [`ARCHITECTURE.md`](ARCHITECTURE.md), [`ADR.md`](ADR.md) e a auditoria
> [`av2/auditoria-final.md`](av2/auditoria-final.md).
>
> Revisado em 15/11/2026. Legenda: `[x]` feito e verificável no código · `[ ]` não feito ·
> **(mudou)** o plano original foi substituído por outra solução.

## Situação atual

- [x] Domínio com regras RN1–RN5 e estados do procedimento
- [x] API com DTOs, tratamento central de erros e rotas de cadastro, planejamento e contagem
- [x] Banco SQLite (EF Core) criado automaticamente
- [x] Duas balanças simuladas escolhidas por configuração
- [x] 87 testes automatizados (domínio, exceções, API e persistência)
- [x] Documentação da AV2 em `docs/av2/`
- [x] Front-end com login, procedimento atual, novo procedimento, operação e cadastros, ligado às rotas do back
- [ ] Câmera integrada à conferência
- [ ] Relatório em PDF
- [ ] Controle de acesso por usuário e perfil
- [ ] Hardware real

## Fases

| Fase | Entrega | Situação |
|---|---|---|
| 0 | Base e documentação | Feita |
| 1 | Modelo de domínio | Feita (com mudanças de plano) |
| 2 | Hardware (interfaces + simulados) | Feita para a balança; câmera parcial |
| 3 | Regras de negócio | Feita (no domínio, não em serviços) |
| 4 | Persistência | Feita |
| 5 | API | Feita, exceto autenticação nas rotas |
| 6 | Front-end | Parcial |
| 7 | Relatórios | Não feita |
| 8 | Hardware real | Não feita |
| 9 | Entrega e apresentação | Em andamento |

---

## Fase 0 — Base e documentação

- [x] Padronizar nomes de pastas (`docs/` minúscula; `Relatorios`, `Servicos`)
- [x] README na raiz com integrantes, instruções e comando de clone correto
- [x] Credenciais de demonstração marcadas como "somente desenvolvimento"
- [x] `ARCHITECTURE.md`, `ADR.md` e `ROADMAP.md` em `docs/`
- [x] Decidir banco (ADR-007 aceito)
- [ ] Decidir autenticação (ADR-008 continua proposto)

## Fase 1 — Modelo de domínio

- [x] `Instrumento` (nome, código, tipo, peso de referência)
- [x] `Procedimento` com estados `Planejado`, `EmAndamento`, `Finalizado`, `Cancelado` **(mudou: o plano chamava de `Cirurgia`)**
- [x] `ItemProcedimento` dentro do `Procedimento` **(mudou: o plano tinha `Contagem` e `ItemContagem`; inicial e final ficaram como quantidades do mesmo item)**
- [x] `Paciente` e `Usuario` (perfil pelo enum `TipoUsuario`) **(mudou: a hierarquia `Pessoa` → `Usuario` → `Cirurgiao`/`Instrumentista`/`Administrador` não foi criada, porque as subclasses não teriam comportamento diferente)**
- [x] Encapsulamento: setters privados/internal e testes de auditoria

## Fase 2 — Hardware

- [x] Interfaces `IBalanca` e `ICamera`
- [x] `BalancaSimulada`, `BalancaComRuido` e `CameraSimulada`
- [x] Balança escolhida por configuração e injetada (`Balanca:Tipo`)
- [ ] Câmera registrada e usada pela API

## Fase 3 — Regras de negócio

- [x] RN1–RN5 nos métodos de `Procedimento` e `ItemProcedimento` **(mudou: o plano previa `CirurgiaService`, `ContagemService` e `ConferenciaPesoService`; ver ADR-011)**
- [x] `ServicoDePesagem` (liga a balança ao procedimento)
- [x] Testes de sucesso e violação para cada regra

## Fase 4 — Persistência

- [x] SQLite + EF Core, banco criado automaticamente com dados de demonstração
- [x] `IRepositorioDeProcedimentos` **(mudou: não há `IRepositorio<T>` genérico; ver ADR-007)**
- [x] Testes de integração com banco temporário
- [ ] Migrações (hoje `EnsureCreated`)

## Fase 5 — API

- [x] DTOs de entrada e saída
- [x] Rotas de login, instrumentos, pacientes, procedimentos (planejar, iniciar, cancelar) e contagem
- [x] Tratamento central de erros e códigos HTTP (ADR-010)
- [x] CORS para `http://localhost:5173`
- [ ] Autenticação e perfis nas rotas (ADR-008)
- [ ] Swagger/OpenAPI

## Fase 6 — Front-end

- [x] Telas: Login, Procedimento atual, Novo procedimento, Operação, Instrumentos e Pacientes **(mudou: o front inicial tinha telas separadas de contagem inicial/final e várias telas vazias; foi substituído por um front novo do grupo)**
- [x] Chamadas HTTP concentradas em `src/api.js`
- [x] Novo procedimento usando `POST /api/procedimentos` e `/iniciar`
- [x] Usuário logado guardado no navegador (`sessionStorage`)
- [x] Compilação verificada com `npm run build`
- [ ] Controlar rotas por perfil
- [ ] Lista/histórico de procedimentos
- [ ] Escolher o instrumentista numa lista (falta `GET /api/usuarios` no back)
- [ ] Câmera ligada ao back (hoje a webcam é só teste visual)

## Fase 7 — Relatórios

- [ ] Rota e tela de relatório final (não existe tela de relatório no front atual)
- [ ] `Relatorio.GerarPDF()` (hoje lança `NotImplementedException`)
- [ ] Gravar o relatório e as assinaturas no banco

## Fase 8 — Hardware real

- [ ] Implementação real de `IBalanca` e `ICamera`
- [ ] Calibrar a tolerância de peso (hoje ±2 g) com instrumentos reais

## Fase 9 — Entrega e apresentação

- [x] README atualizado
- [x] Diagrama de classes conferido com o código (`docs/av2/modelo-classes.md`)
- [x] Mapeamento dos critérios para evidências (`docs/av2/evidencias.json`)
- [ ] Critério G02: alteração feita com o DsCode
- [ ] Demonstração do fluxo completo pela interface

---

## Riscos

| Risco | Impacto | Mitigação |
|---|---|---|
| Hardware indisponível ou instável | Alto | Simuladores e `FalhaDeHardwareException` → 503 (ADR-006, ADR-010) |
| Reconhecimento por imagem complexo | Alto | Câmera fora da conferência por enquanto; peso já confere |
| Banco desatualizado após mudança de modelo | Médio | Apagar `scanops.db`; adotar migrações |
| Rotas sem autenticação | Médio | Fechar ADR-008 |
| Documentação desatualizada | Médio | Atualizar docs junto de cada entrega (já aconteceu uma vez; ver ADR-009) |
