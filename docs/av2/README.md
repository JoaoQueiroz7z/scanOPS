# Documentação da AV2

| Documento | Conteúdo | Critérios |
|---|---|---|
| [especificacao.md](especificacao.md) | Fluxos F1–F3, regras RN1–RN6, estados e critérios de aceitação | G01, A03, A05 |
| [modelo-classes.md](modelo-classes.md) | Diagrama de classes (Mermaid) derivado do código, multiplicidade, composição, contrato, invariante | B01–B07 |
| [analise-modelo.md](analise-modelo.md) | Duas decisões de modelagem com alternativas comparadas | B08 |
| [tratamento-de-falhas.md](tratamento-de-falhas.md) | Três situações de falha, caminho da exceção e testes | D01–D04 |
| [revisao-codigo.md](revisao-codigo.md) | Revisão do commit `eb9a8ed` | C01–C06 |
| [registro-depuracao.md](registro-depuracao.md) | Defeito, hipóteses, técnica, causa raiz, correção e verificação | E01–E06 |
| [rastreabilidade.md](rastreabilidade.md) | Matriz fluxo/regra → código → testes | F05, F01 |
| [auditoria-final.md](auditoria-final.md) | O que está atendido, evidências e pendências | F06 |
| [evidencias.json](evidencias.json) | Índice critério → arquivos e testes | todos |
| [evidencias/](evidencias/) | Saídas reais de execução (API, testes, logs) usadas como prova | C, D, E, F |

Como verificar tudo: `dotnet build` e `dotnet test` na raiz do repositório (ver [README principal](../../README.md)).
