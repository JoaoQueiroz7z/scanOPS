# scanOPS — Sistema de Controle de Instrumentos Cirúrgicos

Sistema que ajuda a evitar que instrumentos cirúrgicos sejam esquecidos dentro do paciente. A conferência usa **duas métricas ao mesmo tempo**: peso (balança) e reconhecimento por imagem (câmera).

> Projeto acadêmico — Design e Programação Orientados a Objetos (UNIFESO).

## Integrantes

- João Victor Faria de Queiroz — 06013779
- Romulo Guedes Medeiros Junior — 06014807
- Luiz Eduardo Ferreira Araujo — 06014155
- Arthur Jorge de Godoy Silva — 06013964
- Thiago de Lima Ramos — 06014301

## Como funciona

1. Antes da cirurgia, os instrumentos são contados (**contagem inicial**): a balança mede o peso e a câmera identifica cada peça.
2. Ao final da cirurgia, é feita uma nova contagem (**contagem final**).
3. O sistema compara as duas contagens e aponta os instrumentos que estão faltando.
4. Se o peso medido não bater com o peso de referência do instrumento (dentro de uma tolerância), o item é sinalizado.
5. Um procedimento com divergências só pode ser finalizado com uma justificativa.

## Arquitetura

```
React (Vite) ──HTTP/JSON──> API em C# ──> regras de negócio (domínio)
                                      ├─> banco SQLite (EF Core)
                                      └─> balança e câmera (simuladas)
```

- **Back-end:** C# / .NET 10 (API web) com as regras de negócio.
- **Banco de dados:** SQLite local via Entity Framework Core.
- **Front-end:** React + Vite, interface para o operador.
- **Hardware:** balança e câmera, acessadas somente pelo back-end através de interfaces (por enquanto, apenas simuladas).

## Estrutura do projeto

```
scanOPS/
├── Api/            # tratamento central de exceções da API
├── Cirurgias/      # Procedimento e seu status
├── Contagem/       # itens contados e status do item
├── Excecoes/       # exceção de regra de negócio
├── Hardware/       # interfaces e simulações de balança e câmera
├── Instrumentos/   # cadastro de instrumentos
├── Persistencia/   # banco SQLite (EF Core) e repositório
├── Pessoas/        # paciente e usuários
├── Relatorios/     # relatório do procedimento
├── Servicos/       # pesagem e demonstração do fluxo
├── docs/           # arquitetura, decisões (ADR), roadmap e evidências (docs/av2)
├── frontend/       # aplicação React (Vite)
├── tests/
│   └── ScanOPS.Tests/  # testes automatizados (xUnit)
├── Program.cs      # API (rotas)
├── appsettings.json # configuração (banco e tipo de balança)
├── scanOPS.csproj  # projeto da API
└── scanOPS.sln     # solução: agrupa API + testes
```

## Como executar

Pré-requisitos: [.NET SDK 10](https://dotnet.microsoft.com/download) e [Node.js](https://nodejs.org).

```bash
git clone https://github.com/JoaoQueiroz7z/scanOPS.git
cd scanOPS
```

**Compilar e testar** (na raiz do repositório):

```bash
dotnet build   # compila a API e o projeto de testes (usa scanOPS.sln)
dotnet test    # roda todos os testes automatizados
```

**Back-end** (terminal 1):

```bash
dotnet run --urls http://localhost:5000
```

**Front-end** (terminal 2):

```bash
cd frontend
npm install
npm run dev
```

Acesse `http://localhost:5173`.

**Conta de demonstração (somente desenvolvimento):** `medico@scanops.com` / `123456`

### Banco de dados

Não é preciso instalar nada: o banco é um arquivo **SQLite** criado automaticamente.

- Na primeira vez que a API sobe (`dotnet run`), ela cria `scanops.db` na raiz do projeto,
  com as tabelas e os dados de demonstração (usuário acima, dois instrumentos e um procedimento em andamento).
- Para recomeçar do zero, pare a API e apague o arquivo `scanops.db` (e `scanops.db-shm`/`scanops.db-wal`, se existirem).
- O banco é criado com `EnsureCreated` (sem migrações): ele **não é atualizado** quando o modelo muda.
  Depois de baixar uma versão que altere as classes gravadas, apague o `scanops.db` para ele ser recriado.
- O caminho do banco fica em `appsettings.json` → `ConnectionStrings:ScanOps`.
- Os testes não usam esse arquivo: cada teste de API cria um banco temporário próprio e o apaga ao final.

### Configuração da balança

Em `appsettings.json` → `Balanca:Tipo`: `Simulada` (peso exato) ou `ComRuido` (simula a imprecisão de uma balança real).
Também é possível trocar sem editar o arquivo: `$env:Balanca__Tipo = "ComRuido"` (PowerShell) antes do `dotnet run`.

## Status atual

- [x] Estrutura inicial do projeto
- [x] Classes do modelo (Procedimento, ItemProcedimento, Instrumento, Paciente, Usuario, Relatorio)
- [x] Conferência de contagens e de peso (no domínio)
- [x] API com rotas de login, instrumentos, pacientes e procedimento em andamento
- [x] Front-end React com as telas principais
- [x] Hardware simulado: duas balanças (`BalancaSimulada` e `BalancaComRuido`, escolhida por configuração) e câmera
- [x] Tratamento central de erros na API (regra de negócio → 400, equipamento indisponível → 503)
- [x] Testes automatizados (xUnit) em `tests/ScanOPS.Tests`: domínio, exceções e integração com a API
- [x] Banco de dados SQLite local (EF Core), criado automaticamente
- [ ] Integração com balança e câmera reais
- [ ] Geração de relatório em PDF

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md)
- [Decisões de arquitetura (ADR)](docs/ADR.md)
- [Roadmap](docs/ROADMAP.md)
