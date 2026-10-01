# scanOPS — Sistema de Controle de Instrumentos Cirúrgicos

CADASTRO 

Usuário/e-mail: medico@scanops.com
Senha: 123456

Sistema que ajuda a evitar que instrumentos cirúrgicos sejam esquecidos dentro do paciente. A conferência usa **duas métricas ao mesmo tempo**: peso (balança) e reconhecimento por imagem (câmera).

> 🚧 Projeto acadêmico em desenvolvimento.

## Como funciona

1. Antes da cirurgia, os instrumentos são contados (**contagem inicial**): a balança mede o peso e a câmera identifica cada peça.
2. Ao final da cirurgia, é feita uma nova contagem (**contagem final**).
3. O sistema compara as duas contagens e aponta os instrumentos que estão faltando.
4. Se o peso medido não bater com o peso de referência do instrumento, o item é sinalizado.

## Arquitetura

```
React (Vite) ──HTTP/JSON──> API em C# ──> balança, câmera, banco de dados
```

- **Back-end:** C# / .NET 10 (API web), regras de negócio e integração com o hardware.
- **Front-end:** React + Vite, interface para o operador.
- **Hardware:** balança e câmera, acessadas somente pelo back-end.

## Estrutura do projeto

```
scanOPS/
├── banco de dados/   # persistência
├── Cirurgias/        # cirurgia e seu status
├── Contagem/         # contagem e itens contados
├── Hardware/         # balança e câmera
├── Instrumentos/     # cadastro de instrumentos
├── Pessoas/          # paciente e cirurgião
├── relatorios/       # relatórios gerados
├── servicos/         # regras de negócio
├── frontend/         # aplicação React (Vite)
├── Program.cs
└── scanOPS.csproj
```

## Como executar

Pré-requisitos: [.NET SDK](https://dotnet.microsoft.com/download) e [Node.js](https://nodejs.org).

```bash
git clone https://github.com/SEU-USUARIO/SEU-REPO.git
cd SEU-REPO
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

## Status

- [x] Estrutura inicial do projeto
- [ ] Classes do modelo (Instrumento, Contagem, Cirurgia...)
- [ ] Lógica de comparação de contagens e conferência de peso
- [ ] API (rotas para o front)
- [ ] Front-end em React
- [ ] Integração com balança e câmera
- [ ] Banco de dados
- [ ] Relatórios

## Autor

João Victor — Ciência da Computação, UNIFESO
