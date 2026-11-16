# scanOPS · front-end (React + Vite, JavaScript)

## Rodar
Terminal 1 (back): `dotnet run --urls http://localhost:5000`
Terminal 2 (front):
```
cd frontend            # coloque esta pasta como "frontend" do repositório
npm install
cp .env.example .env   # Windows: copy .env.example .env
npm run dev            # http://localhost:5173
```
Login de teste (README do projeto): medico@scanops.com / 123456

## Telas e rotas usadas
| Tela | Rotas do back |
|---|---|
| Login | POST /api/login |
| Instrumentos / Pacientes (CRUD) | GET/POST/PUT/DELETE /api/instrumentos e /api/pacientes |
| Novo procedimento | POST /api/procedimentos |
| Procedimento atual | GET /api/procedimento-atual, POST /api/procedimentos/{id}/iniciar e /cancelar |
| Operação | POST /api/procedimento-atual/itens/{id}/entrada, /saida, /pesar e /finalizar |

## Ajustes necessários (não deu para ver os DTOs do back)
- `src/mapeadores.js`: nomes dos campos da resposta de procedimento/itens (use "Ver JSON bruto" na tela de operação).
- `src/pages/Cadastros.jsx`: `CAMPOS_INSTRUMENTO` e `CAMPOS_PACIENTE` (chaves dos DTOs).
- Sugestão pro back: criar `GET /api/usuarios` (escolher cirurgião/instrumentista) e `GET /api/procedimentos` (histórico).
