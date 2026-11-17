# scanOPS · front-end (React + Vite, JavaScript)

## Rodar

Terminal 1 (back, na raiz do repositório): `dotnet run --urls http://localhost:5000`

Terminal 2 (front):

```
cd frontend
npm install
npm run dev            # http://localhost:5173
```

O endereço da API é `http://localhost:5000` por padrão. Para usar outro, copie `.env.example` para `.env`
e ajuste `VITE_API_URL`.

Login de demonstração: `medico@scanops.com` / `123456`.

## Telas e rotas usadas

| Tela | Rotas do back |
|---|---|
| Login | `POST /api/login` |
| Instrumentos / Pacientes (cadastro) | `GET/POST/PUT/DELETE /api/instrumentos` e `/api/pacientes` |
| Novo procedimento (planejar e iniciar) | `POST /api/procedimentos`, `POST /api/procedimentos/{id}/iniciar` |
| Salas em uso (página inicial) | `GET /api/salas/em-uso`, `GET /api/procedimentos/planejados`, `POST /api/procedimentos/{id}/iniciar` e `/cancelar` |
| Histórico (lista e detalhe) | `GET /api/procedimentos/historico`, `GET /api/procedimentos/{id}` |
| Operação (`/operacao/{id}`: contagem e finalização) | `GET /api/procedimentos/{id}`, `POST /api/procedimentos/{id}/itens/{itemId}/entrada`, `/saida`, `/pesar` e `POST /api/procedimentos/{id}/finalizar` |

Os nomes dos campos seguem os DTOs do back (`Api/Dtos/`); a conversão das respostas fica em `src/mapeadores.js`.
Erros do back chegam como `{ "erro": "mensagem" }` e são mostrados na tela.

## Limitações conhecidas

- A câmera da tela de Operação é só um teste visual com a webcam do navegador; não participa da conferência.
- O histórico é só de leitura: não há como editar nem excluir um procedimento.
- O instrumentista é informado pelo id (o back ainda não tem `GET /api/usuarios`); o cirurgião é o usuário logado.
- O login é guardado só no navegador (`sessionStorage`); o back não exige autenticação nas rotas.
- Não existe tela de relatório final.
