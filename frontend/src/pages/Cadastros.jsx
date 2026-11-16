import { useCallback, useEffect, useState } from 'react'
import { instrumentos, pacientes } from '../api.js'

// CRUD genérico. Os campos de cada cadastro estão em CAMPOS abaixo: ajuste "chave" para bater com os DTOs do back.
function Crud({ titulo, api, campos, singular }) {
  const [lista, setLista] = useState([])
  const [form, setForm] = useState(null) // null = fechado; {} = novo; {id,...} = edição
  const [erro, setErro] = useState('')
  const carregar = useCallback(() => api.listar().then(setLista).catch(e => setErro(e.message)), [api])
  useEffect(() => { carregar() }, [carregar])

  async function salvar(e) {
    e.preventDefault(); setErro('')
    try {
      const { id, ...dados } = form
      await (id ? api.atualizar(id, dados) : api.criar(dados))
      setForm(null); carregar()
    } catch (err) { setErro(err.message) }
  }
  async function excluir(item) {
    if (!confirm(`Excluir "${item[campos[0].chave]}"?`)) return
    setErro('')
    try { await api.excluir(item.id); carregar() } catch (err) { setErro(err.message) }
  }
  const valor = (item, c) => (c.tipo === 'checkbox' ? (item[c.chave] ? 'Sim' : 'Não') : item[c.chave] ?? '—')

  return (
    <>
      <header><h1>{titulo}</h1><button className="btn primario" onClick={() => { setErro(''); setForm(Object.fromEntries(campos.map(c => [c.chave, c.tipo === 'checkbox' ? true : '']))) }}>Novo {singular}</button></header>
      {erro && <p className="erro" role="alert">{erro}</p>}
      {form && (
        <form className="cartao form" onSubmit={salvar}>
          <h2>{form.id ? `Editar ${singular}` : `Novo ${singular}`}</h2>
          {campos.map(c => c.tipo === 'checkbox'
            ? <label key={c.chave} className="check"><input type="checkbox" checked={!!form[c.chave]} onChange={e => setForm({ ...form, [c.chave]: e.target.checked })} />{c.rotulo}</label>
            : <label key={c.chave}>{c.rotulo}<input type={c.tipo} step={c.tipo === 'number' ? 'any' : undefined} required={c.obrigatorio} value={form[c.chave] ?? ''}
                onChange={e => setForm({ ...form, [c.chave]: c.tipo === 'number' && e.target.value !== '' ? Number(e.target.value) : e.target.value })} /></label>)}
          <div className="linha"><button className="btn primario">Salvar</button><button type="button" className="btn claro" onClick={() => setForm(null)}>Cancelar</button></div>
        </form>
      )}
      <table>
        <thead><tr>{campos.map(c => <th key={c.chave}>{c.rotulo}</th>)}<th>Ações</th></tr></thead>
        <tbody>
          {lista.map(item => (
            <tr key={item.id}>
              {campos.map(c => <td key={c.chave}>{valor(item, c)}</td>)}
              <td><button className="link" onClick={() => { setErro(''); setForm(item) }}>Editar</button> <button className="link vermelho" onClick={() => excluir(item)}>Excluir</button></td>
            </tr>
          ))}
          {!lista.length && <tr><td colSpan={campos.length + 1}>Nenhum registro ainda.</td></tr>}
        </tbody>
      </table>
    </>
  )
}

const CAMPOS_INSTRUMENTO = [
  { chave: 'nome', rotulo: 'Nome', tipo: 'text', obrigatorio: true },
  { chave: 'pesoReferencia', rotulo: 'Peso de referência (g)', tipo: 'number', obrigatorio: true },
  { chave: 'ativo', rotulo: 'Ativo', tipo: 'checkbox' },
]
const CAMPOS_PACIENTE = [
  { chave: 'nome', rotulo: 'Nome', tipo: 'text', obrigatorio: true },
  { chave: 'prontuario', rotulo: 'Prontuário', tipo: 'text' },
]

export const Instrumentos = () => <Crud titulo="Instrumentos" singular="instrumento" api={instrumentos} campos={CAMPOS_INSTRUMENTO} />
export const Pacientes = () => <Crud titulo="Pacientes" singular="paciente" api={pacientes} campos={CAMPOS_PACIENTE} />
