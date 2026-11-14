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
                                      └─> balança e câmera (simuladas)
```

- **Back-end:** C# / .NET 10 (API web) com as regras de negócio.
- **Front-end:** React + Vite, interface para o operador.
- **Hardware:** balança e câmera, acessadas somente pelo back-end (por enquanto, apenas simuladas).

## Estrutura do projeto

```
scanOPS/
├── Cirurgias/      # Procedimento e seu status
├── Contagem/       # itens contados e status do item
├── Hardware/       # interfaces e simulações de balança e câmera
├── Instrumentos/   # cadastro de instrumentos
├── Pessoas/        # paciente e usuários
├── Relatorios/     # relatório do procedimento
├── Servicos/       # demonstração do fluxo
├── docs/           # arquitetura, decisões (ADR) e roadmap
├── frontend/       # aplicação React (Vite)
├── Program.cs      # API (rotas)
└── scanOPS.csproj
```

## Como executar

Pré-requisitos: [.NET SDK 10](https://dotnet.microsoft.com/download) e [Node.js](https://nodejs.org).

```bash
git clone https://github.com/JoaoQueiroz7z/scanOPS.git
cd scanOPS
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

## Status atual

- [x] Estrutura inicial do projeto
- [x] Classes do modelo (Procedimento, ItemProcedimento, Instrumento, Paciente, Usuario, Relatorio)
- [x] Conferência de contagens e de peso (no domínio)
- [x] API com rotas de login, instrumentos, pacientes e procedimento em andamento (dados em memória)
- [x] Front-end React com as telas principais
- [x] Hardware simulado (balança e câmera)
- [ ] Testes automatizados
- [ ] Banco de dados (os dados ainda ficam apenas em memória)
- [ ] Integração com balança e câmera reais
- [ ] Geração de relatório em PDF

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md)
- [Decisões de arquitetura (ADR)](docs/ADR.md)
- [Roadmap](docs/ROADMAP.md)
