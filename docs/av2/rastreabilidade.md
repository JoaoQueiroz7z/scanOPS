# Matriz de rastreabilidade

Liga cada fluxo e cada regra da [especificação](especificacao.md) à implementação e aos testes
automatizados. Todos os testes ficam em `tests/ScanOPS.Tests/` e rodam com `dotnet test`.
Arquivos de teste: `Regras/RegrasDeNegocioTests.cs` (RegrasNeg), `Regras/InvarianteProcedimentoTests.cs` (Invariante),
`Dominio/ProcedimentoContagemTests.cs` (Contagem), `Excecoes/SituacoesDeFalhaTests.cs` (Falhas),
`Api/PersistenciaIntegracaoTests.cs` (Persistência), `Api/CicloDeVidaProcedimentoApiTests.cs` (CicloApi),
`Api/ContagemAposFinalizarTests.cs` (ContagemApi), `Hardware/PolimorfismoBalancaTests.cs` (Balança).

## 1. Regras de negócio

| Regra | Implementação | Usada pela API em | Testes de sucesso | Testes de violação |
|---|---|---|---|---|
| **RN1** iniciar só se Planejado | `Procedimento.Iniciar()` — `Cirurgias/Procedimento.cs` | `POST /api/procedimentos/{id}/iniciar` | RegrasNeg `RN1_Sucesso_IniciarPlanejado_FicaEmAndamento`; CicloApi `RN1_Iniciar_ProcedimentoPlanejado_FicaEmAndamentoGravadoEViraOProcedimentoAtual` | RegrasNeg `RN1_Violacao_IniciarJaEmAndamento_LancaEMantemDataDeInicio`, `RN1_Violacao_IniciarCancelado_Lanca`; CicloApi `RN1_IniciarDuasVezes_Retorna400EMantemADataDeInicioGravada` |
| **RN2** finalizar só se EmAndamento | `Procedimento.Finalizar()` (1ª verificação) | `POST /api/procedimento-atual/finalizar` | RegrasNeg `RN2_Sucesso_FinalizarEmAndamentoSemDivergencia_FicaFinalizado` | RegrasNeg `RN2_Violacao_FinalizarPlanejado_LancaEMantemPlanejado`, `RN2_Violacao_FinalizarDuasVezes_LancaEMantemDataDeFim` |
| **RN3** divergência exige justificativa | `Procedimento.Finalizar()` (2ª verificação, antes de alterar estado) | `POST /api/procedimento-atual/finalizar` | RegrasNeg `RN3_Sucesso_DivergenciaComJustificativa_FinalizaERegistraJustificativa`; Persistência `ContagemEFinalizacaoComJustificativa_SaoGravadasNoBanco` | RegrasNeg `RN3_Violacao_DivergenciaSemJustificativa_Lanca` (null, "", "   "); Falhas `F1_Dominio_...`, `F1_Api_...`; Persistência `FinalizacaoRecusada_NaoGravaNenhumaAlteracaoNoBanco` |
| **RN4** peso com tolerância de ±2 g | `ItemProcedimento.ConferePeso()` e `PossuiDivergencia()` — `Contagem/ItemProcedimento.cs` | `GET /api/procedimento-atual` (status do item) e `/finalizar` | RegrasNeg `RN4_Sucesso_PesoDentroDaTolerancia_ItemSemDivergencia` (98, 100, 102 g); Balança `MesmoServico_ComQualquerBalanca_GravaPesoQueRespeitaRN4` | RegrasNeg `RN4_Violacao_PesoForaDaTolerancia_ItemDivergenteEFinalizarExigeJustificativa` (102,01 e 50 g) |
| **RN5** contagem só EmAndamento e com valores válidos | `Procedimento.RegistrarEntrada/RegistrarSaida/RegistrarPeso` + `GarantirContagemPermitida()` | `POST /api/procedimento-atual/itens/{id}/entrada`, `/saida`, `/pesar` | RegrasNeg `RN5_Sucesso_EntradaESaidaEmAndamento_AtualizamContagem`; Contagem `RegistrarEntrada_EmAndamentoComQuantidadePositiva_SomaNaContagemInicial`, `RegistrarSaida_QuantidadeZero_EhAceita` | RegrasNeg `RN5_Violacao_EntradaComProcedimentoPlanejado_Lanca`, `RN5_Violacao_PesoNegativoNaSaida_LancaENaoAlteraNada`; Contagem `RegistrarEntrada_ProcedimentoFinalizado_...`, `RegistrarEntrada_QuantidadeMenorOuIgualAZero_...` (0 e −5), `RegistrarSaida_QuantidadeNegativa_...`; ContagemApi `RegistrarEntrada_DepoisDeFinalizado_...`, `RegistrarEntrada_QuantidadeNegativa_...`; Persistência `ContagemAposFinalizar_ERecusadaEOBancoContinuaComOsValoresFinais` |

| **RN6** instrumento inativo não entra em procedimento novo | `Procedimento.AdicionarInstrumento()` (verifica `Instrumento.Ativo`); `Instrumento.Inativar()/Reativar()` | `POST /api/procedimentos`; `POST /api/instrumentos/{id}/inativar` e `/reativar` | RegrasNeg `RN6_Sucesso_InstrumentoAtivo_EhAdicionado`, `RN6_Sucesso_InstrumentoReativado_VoltaASerAceito`; `InstrumentoAtivoTests` (5 testes) | RegrasNeg `RN6_Violacao_InstrumentoInativo_LancaENaoAlteraLista`; CicloApi `RN6_PlanejarComInstrumentoInativo_Retorna400ENadaEGravado_EReativadoFunciona` |

| **RN7** uma sala, um procedimento em andamento | `Servicos/ServicoDeInicioDeProcedimento.Iniciar()` + `IRepositorioDeProcedimentos.ObterEmAndamentoNaSala()`; `Cirurgias/SalaCirurgica.Padronizar()`; índice único parcial em `Persistencia/ScanOpsDbContext.cs` | `POST /api/procedimentos/{id}/iniciar` | Rn7 `RN7_SalaLivre_IniciaEGravaEmAndamento`, `RN7_DepoisDeFinalizar_SalaFicaLivreDeNovo`, `RN7_ProcedimentoCanceladoNaoOcupaASala`; Sala `Padronizar_DiferentesFormas_GravaComoSalaX` | Rn7 `RN7_SalaOcupada_Recusa400ComMensagemENadaMudaNoBanco`, `RN7_SalaEscritaDeOutroJeito_EhAMesmaSala` (3 formas), `RN7_IndiceDoBanco_BarraDoisEmAndamentoNaMesmaSalaComoRegraENaoErro500` |

Total: **7 regras × (sucesso + violação)**, com mais de 10 cenários verificáveis (critério F01).
Arquivos novos desta etapa: `Api/SalasRn7Tests.cs` (Rn7), `Dominio/SalaCirurgicaTests.cs` (Sala),
`Api/HistoricoESalasTests.cs` (Hist), `Dominio/HistoricoDominioTests.cs` (HistDom).

## 2. Fluxos

| Fluxo | Passos (rotas) | Regras envolvidas | Classes que colaboram | Testes de ponta a ponta |
|---|---|---|---|---|
| **F1** Planejar, iniciar e contagem inicial | `POST /api/procedimentos` → `POST /api/procedimentos/{id}/iniciar` → `POST /api/procedimentos/{id}/itens/{itemId}/entrada` | RN1, RN5, RN6, RN7 | `Procedimento`, `ItemProcedimento`, `Instrumento`, `Paciente`, `Usuario`, `SalaCirurgica`, `ServicoDeInicioDeProcedimento`, `RepositorioDeProcedimentos` | CicloApi `Planejar_CriaProcedimentoPlanejadoComOsItensPrevistos`, `RN1_Iniciar_...`; Persistência `ContagemEFinalizacaoComJustificativa_SaoGravadasNoBanco` (entrada gravada) |
| **F2** Contagem final e conferência de peso | `POST /api/procedimentos/{id}/itens/{itemId}/saida` e/ou `.../pesar` → `GET /api/procedimentos/{id}` | RN4, RN5 | `Procedimento`, `ItemProcedimento`, `Instrumento` (peso de referência), `ServicoDePesagem`, `IBalanca` | Falhas `Api_PesarComBalancaFuncionando_Retorna200EGravaPesoNoItem`, `F3_Api_ErroNaBalanca_...`; Balança `MesmoServico_ComQualquerBalanca_...` |
| **F3** Finalização com/sem justificativa | `POST /api/procedimentos/{id}/finalizar` | RN2, RN3, RN4 | `Procedimento`, `ItemProcedimento` | Persistência `ContagemEFinalizacaoComJustificativa_SaoGravadasNoBanco`, `FinalizacaoRecusada_NaoGravaNenhumaAlteracaoNoBanco`; Falhas `F1_Api_...`, `F2_Api_...`; Hist `Detalhe_FinalizadoTrazTodasAsInformacoesEOsItens` |
| **F4** Histórico e salas em uso | `GET /api/salas/em-uso`, `GET /api/procedimentos/historico`, `GET /api/procedimentos/{id}` | RN7; invariante do histórico | `Procedimento`, `ItemProcedimento` (foto), `RepositorioDeProcedimentos` | Hist `Historico_ListaSoFinalizadosECancelados_MaisRecentePrimeiro`, `Procedimento_NaoTemRotaDeEdicaoNemDeExclusao`, `ProcedimentoFinalizado_RecusaTodaOperacaoPelaApi`, `ExcluirPacienteUsadoEmProcedimento_Retorna400EPacientePermanece`, `Foto_InstrumentoRenomeadoEComPesoCorrigidoDepois_NaoMudaOItem`, `SalasEmUso_MostraCadaSalaOcupadaComSeuProcedimento` |

As rotas antigas `/api/procedimento-atual/...` usam as mesmas funções das rotas por id e continuam
cobertas pelos testes anteriores (`ContagemAposFinalizarTests`, `SituacoesDeFalhaTests`, `PersistenciaIntegracaoTests`).

## 3. Invariante, exceções e polimorfismo

| Requisito | Implementação | Testes |
|---|---|---|
| Invariante do agregado (registro fechado não muda; recusa não deixa estado parcial) | `Procedimento` (`private set`, `GarantirContagemPermitida`, validação antes de alterar em `Finalizar`); `ItemProcedimento` com membros `internal` | Invariante `ProcedimentoFinalizado_RecusaTodaAlteracaoEPermaneceIntacto`, `FinalizarRecusadoPorDivergencia_NaoAlteraNenhumEstado`, auditoria de encapsulamento (3 testes) |
| Três situações de falha | `RegraDeNegocioException`, `FalhaDeHardwareException`, `Api/TratadorDeExcecoes.cs` | Falhas (8 testes) — ver [tratamento-de-falhas.md](tratamento-de-falhas.md) |
| Contrato `IBalanca` com 2 implementações | `Hardware/BalancaSimulada.cs`, `Hardware/BalancaComRuido.cs`, `Hardware/ConfiguracaoDeHardware.cs` | Balança (8 testes) |
| Persistência | `Persistencia/` (EF Core + SQLite) | Persistência (5 testes), CicloApi (7 testes) |

## 4. Como conferir

```bash
dotnet test                                   # todos os testes
dotnet test --filter "FullyQualifiedName~RN3"  # só os testes de uma regra
dotnet test --list-tests                      # lista os nomes citados acima
```
