// Todas as chamadas ao back-end (C# / .NET) ficam aqui. Rotas conforme o Program.cs do repositório.
const BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000'

export class ApiError extends Error {
  constructor(msg, status) { super(msg); this.status = status }
}

async function http(path, { method = 'GET', body } = {}) {
  let res
  try {
    res = await fetch(BASE + path, { method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) })
  } catch {
    throw new ApiError(`Não foi possível conectar à API em ${BASE}. Ela está rodando?`, 0)
  }
  const texto = await res.text()
  let dados = null
  try { dados = texto ? JSON.parse(texto) : null } catch { dados = null } // resposta que não é JSON
  // O back responde erros como { "erro": "mensagem" } (Api/TratadorDeExcecoes.cs).
  // detail/title ficam como reserva caso algum erro venha no formato padrão do ASP.NET.
  if (!res.ok) throw new ApiError(dados?.erro || dados?.detail || dados?.title || `Erro ${res.status}`, res.status)
  return dados
}

export const login = (email, senha) =>
  http('/api/login', { method: 'POST', body: { email, senha } }).catch(e => {
    throw e.status === 401 ? new ApiError('E-mail ou senha incorretos.', 401) : e
  })

// CRUD genérico: /api/instrumentos e /api/pacientes
export const crud = base => ({
  listar: () => http(`/api/${base}`),
  criar: d => http(`/api/${base}`, { method: 'POST', body: d }),
  atualizar: (id, d) => http(`/api/${base}/${id}`, { method: 'PUT', body: d }),
  excluir: id => http(`/api/${base}/${id}`, { method: 'DELETE' }),
})
export const instrumentos = crud('instrumentos')
export const pacientes = crud('pacientes')

export const procedimentos = {
  criar: d => http('/api/procedimentos', { method: 'POST', body: d }),
  atual: () => http('/api/procedimento-atual'),
  iniciar: id => http(`/api/procedimentos/${id}/iniciar`, { method: 'POST' }),
  cancelar: (id, motivo) => http(`/api/procedimentos/${id}/cancelar`, { method: 'POST', body: { motivo } }),
  entrada: (itemId, quantidade) => http(`/api/procedimento-atual/itens/${itemId}/entrada`, { method: 'POST', body: { quantidade } }),
  saida: (itemId, d) => http(`/api/procedimento-atual/itens/${itemId}/saida`, { method: 'POST', body: d }),
  pesar: itemId => http(`/api/procedimento-atual/itens/${itemId}/pesar`, { method: 'POST' }),
  finalizar: justificativa => http('/api/procedimento-atual/finalizar', { method: 'POST', body: { justificativa } }),
}
