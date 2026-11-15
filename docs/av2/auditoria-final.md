# Auditoria final

Correspondência entre os critérios da AV2, a implementação, os testes e os resultados. Feita sobre a
versão desta branch; os testes foram executados com `dotnet test` (**85 aprovados, 0 falhas**) e o
build com `dotnet build` (0 erros, 0 avisos). Índice das evidências por critério:
[evidencias.json](evidencias.json).

Legenda: **Atendido** = existe implementação e evidência verificável no repositório.
**Pendente** = ainda não existe, ou existe só parcialmente; não é declarado como concluído.

## 1. Critérios

| Critério | Situação | Evidência principal |
|---|---|---|
| A01 Cinco classes de domínio | Atendido | `Procedimento`, `ItemProcedimento`, `Instrumento`, `Paciente`, `Usuario`, `Relatorio` |
| A02 Comportamento próprio em 2+ classes | Atendido | `Procedimento.Finalizar`, `ItemProcedimento.ConferePeso`, `Usuario.Autenticar`, `Paciente.GetIdade` |
| A03 Cinco regras implementadas e usadas | Atendido | RN1–RN5 em `Procedimento`/`ItemProcedimento`, chamadas pelas rotas de `Program.cs` |
| A04 Colaboração entre classes | Atendido | F2/F3: `ServicoDePesagem` → `IBalanca` → `Procedimento` → `ItemProcedimento` → `Instrumento.PesoReferencia` |
| A05 Objeto com 3+ estados e transições | Atendido | `StatusProcedimento` (4 estados); `Identidade_TransicoesPreservamOMesmoId` |
| A06 Transição proibida rejeitada sem inconsistência | Atendido | `FinalizarRecusadoPorDivergencia_NaoAlteraNenhumEstado`, `FinalizacaoRecusada_NaoGravaNenhumaAlteracaoNoBanco`; `evidencias/invariante-finalizar.txt` |
| A07 Regras protegidas por encapsulamento | Atendido | membros `internal`/`private set`; testes de auditoria de encapsulamento |
| A08 Polimorfismo real | Atendido | `IBalanca` com `BalancaSimulada` e `BalancaComRuido`, escolhida por configuração e usada pela rota `/pesar` |
| B01–B07 Modelo de classes | Atendido | [modelo-classes.md](modelo-classes.md) (Mermaid, correspondência, multiplicidade, composição, contrato, invariante) |
| B08 Duas decisões de modelagem | Atendido | [analise-modelo.md](analise-modelo.md) |
| C01–C06 Revisão de uma alteração | Atendido | [revisao-codigo.md](revisao-codigo.md) sobre `eb9a8ed`; `evidencias/revisao-verificacao.txt` |
| D01–D04 Exceções | Atendido | [tratamento-de-falhas.md](tratamento-de-falhas.md); `Excecoes/SituacoesDeFalhaTests.cs` |
| E01–E06 Depuração | Atendido | [registro-depuracao.md](registro-depuracao.md); `evidencias/depuracao-log.txt` e pares antes/depois |
| F01 Sucesso e violação por regra (≥10) | Atendido | `Regras/RegrasDeNegocioTests.cs` e [rastreabilidade.md](rastreabilidade.md) |
| F02 Teste que tenta violar invariante | Atendido | `Regras/InvarianteProcedimentoTests.cs` |
| F03 Integração API → regra → persistência | Atendido | `Api/PersistenciaIntegracaoTests.cs`, `Api/CicloDeVidaProcedimentoApiTests.cs` |
| F04 Compila e testa a partir de clone limpo | Atendido | instruções no `README.md`; verificado numa cópia só com os arquivos versionados: build sem erros, 85 testes aprovados, API no ar com banco criado e login funcionando |
| F05 Rastreabilidade | Atendido | [rastreabilidade.md](rastreabilidade.md) |
| F06 Auditoria final | Atendido | este documento |
| G01 Especificação verificável | Atendido | [especificacao.md](especificacao.md) |
| **G02 Especificação para o DsCode** | **Pendente** | a alteração funcional reservada para o DsCode ainda não foi feita |

## 2. Pendências e limitações conhecidas

| Item | Situação real | Impacto |
|---|---|---|
| G02 (DsCode) | Não realizado até esta versão. Alteração reservada: validação do cadastro de instrumento (hoje a API aceita instrumento com peso de referência 0). | Critério G02 não atendido até ser feito |
| Relatório em PDF | `Relatorio.GerarPDF()` lança `NotImplementedException`; não há rota de relatório nem gravação do `Relatorio` no banco | Funcionalidade inexistente; não aparece como concluída no README |
| Câmera | `ICamera`/`CameraSimulada` existem, mas nenhuma rota usa a câmera; `IdentificadoPorCamera` é informado pelo cliente na saída | A "dupla métrica" funciona só com o peso no back-end |
| Hardware real | Apenas balanças simuladas | — |
| Autenticação nas rotas | `/api/login` valida a senha, mas as demais rotas não exigem usuário autenticado nem verificam perfil | Sem controle de acesso |
| Equipe do procedimento | `POST /api/procedimentos` não verifica se o cirurgião tem cargo `Cirurgiao` (nem o instrumentista) | Regra de perfil não implementada |
| Encapsulamento por `internal` | Protege contra outros projetos (os testes comprovam), não contra código dentro do próprio projeto da API | Separar o domínio num projeto próprio fecharia esse limite |
| Banco sem migrações | `EnsureCreated` não atualiza um `scanops.db` existente quando o modelo muda (documentado no README) | Em desenvolvimento, apagar o arquivo |
| Front-end | Não foi compilado nem executado nas verificações (Node.js não instalado na máquina usada). A tela "Novo Procedimento" está vazia e ainda não usa as rotas de planejar/iniciar | O fluxo F1 é executável pela API, não pela interface |
| Documentos de arquitetura em `docs/` | Foram escritos antes da implementação e estavam desatualizados (nomes `Cirurgia`/`ItemContagem`, regras "propostas", banco "a definir"). Revisados em 15/11/2026 para descrever o código; decisões novas registradas como ADR-010 e ADR-011 | Resolvido; o `ROADMAP.md` marca o que mudou de plano |

## 3. Como repetir a auditoria

```bash
git clone https://github.com/JoaoQueiroz7z/scanOPS.git
cd scanOPS
dotnet build
dotnet test
dotnet run --urls http://localhost:5000   # cria scanops.db; login medico@scanops.com / 123456
```
