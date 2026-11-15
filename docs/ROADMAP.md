# scanOPS — Roadmap

> Ordem de construção do sistema. Marque `[x]` ao concluir cada tarefa.
> Veja também: [`ARCHITECTURE.md`](ARCHITECTURE.md) e [`ADR.md`](ADR.md).

## Status atual (segundo o README)

- [x] Estrutura inicial do projeto
- [x] Classes do modelo (Instrumento, Contagem, Cirurgia...)
- [ ] Lógica de comparação de contagens e conferência de peso
- [ ] API (rotas para o front)
- [ ] Front-end em React
- [ ] Integração com balança e câmera
- [ ] Banco de dados
- [ ] Relatórios

## Visão geral das fases

| Fase | Entrega | Conceitos de POO | Depende de |
|---|---|---|---|
| 0 | Base e documentação | — | — |
| 1 | Modelo de domínio | Encapsulamento, herança, composição | 0 |
| 2 | Hardware (interfaces + simulados) | Interfaces, polimorfismo | 1 |
| 3 | Serviços e regras de negócio | Responsabilidade única, injeção de dependência | 1, 2 |
| 4 | Persistência | Generics, padrão Repository | 1, 3 |
| 5 | API | Camadas, DTOs | 3, 4 |
| 6 | Front-end | — | 5 |
| 7 | Relatórios | Strategy, polimorfismo | 3, 4 |
| 8 | Hardware real | Substituição de implementações | 2, 3 |
| 9 | Entrega e apresentação | — | todas |

**Caminho recomendado:** 0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9.
Faça as fases 1 a 3 com hardware simulado e teste a lógica antes de ligar equipamento real.

---

## Fase 0 — Base e documentação

- [ ] Padronizar nomes de pastas (`Relatorios`, `Servicos`) e conferir o README
- [ ] Corrigir o comando de clone do README (`SEU-USUARIO/SEU-REPO`)
- [ ] Revisar credenciais de demonstração do README
- [ ] Commitar `ARCHITECTURE.md`, `ADR.md` e `ROADMAP.md` na raiz
- [ ] Decidir pendências: banco (ADR-007) e autenticação (ADR-008)

**Pronto quando:** repositório limpo, documentos na raiz, projeto compila e roda.

## Fase 1 — Modelo de domínio

- [ ] Classe abstrata `Pessoa` e subclasses `Paciente` e `Usuario`
- [ ] `Cirurgiao`, `Instrumentista`, `Administrador` herdando de `Usuario`
- [ ] `Instrumento` (nome, peso de referência)
- [ ] `Cirurgia` (paciente, cirurgião, status, contagens)
- [ ] `Contagem` e `ItemContagem`
- [ ] Enums: `StatusCirurgia`, `TipoContagem`, `StatusItem`, `PerfilUsuario`
- [ ] Validações nos construtores/propriedades (encapsulamento)

**Pronto quando:** as classes compilam, respeitam o diagrama UML e têm testes simples de criação e validação.

## Fase 2 — Hardware (interfaces e simulados)

- [ ] Interfaces `IBalanca` e `ICamera`
- [ ] `BalancaSimulada` e `CameraSimulada` (valores configuráveis)
- [ ] Registro das implementações em `Program.cs`

**Pronto quando:** é possível obter peso e lista de instrumentos "reconhecidos" sem nenhum equipamento.

## Fase 3 — Serviços e regras de negócio

- [ ] `ContagemService`: executar contagem inicial e final
- [ ] Comparação inicial × final → lista de faltantes (RN2)
- [ ] `ConferenciaPesoService`: peso medido × referência com tolerância (RN3/RN4)
- [ ] `CirurgiaService`: status e encerramento (RN5/RN6)
- [ ] `InstrumentoService`: cadastro e consulta
- [ ] Testes de unidade para RN1–RN6

**Pronto quando:** o fluxo completo roda em teste: contagem inicial → contagem final → resultado com faltantes e divergências.

## Fase 4 — Persistência

- [ ] Definir o banco (fechar ADR-007)
- [ ] Interface `IRepositorio<T>` e repositórios de Instrumento, Cirurgia, Contagem e Pessoas
- [ ] Versão em memória para testes
- [ ] Versão com banco real e migração/script de criação

**Pronto quando:** os dados sobrevivem ao reiniciar a aplicação e os serviços não mudaram.

## Fase 5 — API

- [ ] DTOs de entrada e saída
- [ ] Rotas de autenticação, instrumentos, pacientes, cirurgias e contagens
- [ ] Tratamento de erros e códigos HTTP
- [ ] CORS para `http://localhost:5173`
- [ ] Autenticação e perfis (fechar ADR-008)

**Pronto quando:** todas as rotas respondem corretamente e podem ser testadas por ferramenta HTTP (ex.: Swagger).

## Fase 6 — Front-end (React + Vite)

- [ ] Camada `api/` com as chamadas HTTP
- [ ] Tela **Login** (seguindo o mockup)
- [ ] Telas **Visão Geral** e **Paciente**
- [ ] Telas **Novo Procedimento** e **Instrumentos**
- [ ] Telas **Contagem Inicial**, **Procedimento em Andamento** e **Contagem Final**
- [ ] Tela **Relatório Final**
- [ ] Controle de rotas por perfil de usuário

**Pronto quando:** um operador completa o fluxo inteiro pela interface, com hardware simulado.

## Fase 7 — Relatórios

- [ ] `RelatorioService` monta o resumo da cirurgia (faltantes, divergências, responsáveis)
- [ ] Interface `IGeradorRelatorio` com formato em tela e formato exportável (ex.: PDF)
- [ ] Rota e tela do Relatório Final integradas

**Pronto quando:** o relatório final é gerado e exportado para uma cirurgia de teste.

## Fase 8 — Hardware real

- [ ] Implementação real de `IBalanca`
- [ ] Implementação real de `ICamera` (reconhecimento por imagem)
- [ ] Calibrar tolerância de peso com instrumentos reais (RN4)
- [ ] Alternar entre simulado e real por configuração

**Pronto quando:** a contagem funciona com equipamento real sem alterar serviços nem front.

## Fase 9 — Entrega e apresentação

- [ ] README atualizado (como executar, status, credenciais de demonstração)
- [ ] Diagrama de classes final conferido com o código
- [ ] Demonstração do fluxo completo ensaiada
- [ ] Mapeamento dos pilares de POO para exemplos do código (para a banca)

---

## Riscos

| Risco | Impacto | Mitigação |
|---|---|---|
| Hardware indisponível ou instável | Alto | Simuladores desde a fase 2 (ADR-006) |
| Reconhecimento por imagem complexo | Alto | Começar com identificação simplificada; evoluir depois |
| Banco e autenticação indefinidos | Médio | Fechar ADR-007/008 na fase 0 |
| Código gerado por IA fora do padrão | Médio | Usar o bloco de regras do ARCHITECTURE (seção 11) em todo pedido |
| Documentação desatualizada | Médio | Atualizar docs junto de cada entrega |
