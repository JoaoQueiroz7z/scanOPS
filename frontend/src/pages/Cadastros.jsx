import { useCallback, useEffect, useState } from 'react'
import { instrumentos, pacientes } from '../api.js'

// CRUD genérico. As chaves de CAMPOS são exatamente os campos dos DTOs do back
// (Api/Dtos/InstrumentoDtos.cs e Api/Dtos/PacienteDtos.cs).
// Tipos de campo suportados: text, number, date e select (com "opcoes").
// colunasExtras: colunas só de leitura na tabela (não entram no formulário).
// acaoExtra(item, executar): botão a mais por linha (ex.: Inativar).
function Crud({ titulo, api, campos, singular, colunasExtras = [], acaoExtra }) {
  const [lista, setLista] = useState([])
  const [form, setForm] = useState(null) // null = fechado; {} = novo; {id,...} = edição
  const [erro, setErro] = useState('')
  const carregar = useCallback(() => api.listar().then(setLista).catch(e => setErro(e.message)), [api])
  useEffect(() => { carregar() }, [carregar])

  const vazio = () => Object.fromEntries(campos.map(c => [c.chave, '']))
  const paraForm = item => ({ id: item.id, ...Object.fromEntries(campos.map(c => [c.chave, item[c.chave] ?? ''])) })

  async function salvar(e) {
    e.preventDefault(); setErro('')
    try {
      // Envia só os campos do formulário (o DTO de requisição não tem id, ativo, idade etc.).
      const dados = Object.fromEntries(campos.map(c => [c.chave, form[c.chave]]))
      await (form.id ? api.atualizar(form.id, dados) : api.criar(dados))
      setForm(null); carregar()
    } catch (err) { setErro(err.message) }
  }
  async function excluir(item) {
    if (!confirm(`Excluir "${item[campos[0].chave]}"?`)) return
    setErro('')
    try { await api.excluir(item.id); carregar() } catch (err) { setErro(err.message) }
  }
  async function executar(fn) {
    setErro('')
    try { await fn(); carregar() } catch (err) { setErro(err.message) }
  }

  const valor = (item, c) => {
    const v = item[c.chave]
    if (v === null || v === undefined || v === '') return '—'
    if (c.tipo === 'select') return c.opcoes.find(o => o.valor === v)?.rotulo ?? v
    if (c.tipo === 'date') return v.split('-').reverse().join('/') // aaaa-mm-dd -> dd/mm/aaaa
    return v
  }

  const mudar = c => e => {
    const v = e.target.value
    setForm({ ...form, [c.chave]: c.tipo === 'number' && v !== '' ? Number(v) : v })
  }

  const entrada = c => c.tipo === 'select'
    ? <select required={c.obrigatorio} value={form[c.chave] ?? ''} onChange={mudar(c)}>
        <option value="">Selecione…</option>
        {c.opcoes.map(o => <option key={o.valor} value={o.valor}>{o.rotulo}</option>)}
      </select>
    : <input type={c.tipo} step={c.tipo === 'number' ? 'any' : undefined} required={c.obrigatorio}
        value={form[c.chave] ?? ''} onChange={mudar(c)} />

  return (
    <>
      <header><h1>{titulo}</h1><button className="btn primario" onClick={() => { setErro(''); setForm(vazio()) }}>Novo {singular}</button></header>
      {erro && <p className="erro" role="alert">{erro}</p>}
      {form && (
        <form className="cartao form" onSubmit={salvar}>
          <h2>{form.id ? `Editar ${singular}` : `Novo ${singular}`}</h2>
          {campos.map(c => <label key={c.chave}>{c.rotulo}{entrada(c)}</label>)}
          <div className="linha"><button className="btn primario">Salvar</button><button type="button" className="btn claro" onClick={() => setForm(null)}>Cancelar</button></div>
        </form>
      )}
      <table>
        <thead><tr>{campos.map(c => <th key={c.chave}>{c.rotulo}</th>)}{colunasExtras.map(c => <th key={c.rotulo}>{c.rotulo}</th>)}<th>Ações</th></tr></thead>
        <tbody>
          {lista.map(item => (
            <tr key={item.id}>
              {campos.map(c => <td key={c.chave}>{valor(item, c)}</td>)}
              {colunasExtras.map(c => <td key={c.rotulo}>{c.valor(item)}</td>)}
              <td><button className="link" onClick={() => { setErro(''); setForm(paraForm(item)) }}>Editar</button> {acaoExtra && <>{acaoExtra(item, executar)} </>}<button className="link vermelho" onClick={() => excluir(item)}>Excluir</button></td>
            </tr>
          ))}
          {!lista.length && <tr><td colSpan={campos.length + colunasExtras.length + 1}>Nenhum registro ainda.</td></tr>}
        </tbody>
      </table>
    </>
  )
}

// Valores do enum TipoInstrumento do back (Instrumentos/TipoInstrumento.cs).
const TIPOS_INSTRUMENTO = [
  { valor: 'Cortante', rotulo: 'Cortante' },
  { valor: 'Pinca', rotulo: 'Pinça' },
  { valor: 'Tesoura', rotulo: 'Tesoura' },
  { valor: 'Afastador', rotulo: 'Afastador' },
  { valor: 'Clamp', rotulo: 'Clamp' },
  { valor: 'Canula', rotulo: 'Cânula' },
  { valor: 'Agulha', rotulo: 'Agulha' },
  { valor: 'Outros', rotulo: 'Outros' },
]

// InstrumentoRequisicao: nome, codigo, tipo, material, tamanho, pesoReferencia
const CAMPOS_INSTRUMENTO = [
  { chave: 'nome', rotulo: 'Nome', tipo: 'text', obrigatorio: true },
  { chave: 'codigo', rotulo: 'Código', tipo: 'text' },
  { chave: 'tipo', rotulo: 'Tipo', tipo: 'select', opcoes: TIPOS_INSTRUMENTO, obrigatorio: true },
  { chave: 'material', rotulo: 'Material', tipo: 'text' },
  { chave: 'tamanho', rotulo: 'Tamanho', tipo: 'text' },
  { chave: 'pesoReferencia', rotulo: 'Peso de referência (g)', tipo: 'number', obrigatorio: true },
]

// Mesmos valores de Paciente.SexosValidos e Paciente.TiposSanguineosValidos no back.
// O valor guardado usa hífen comum ("A-"); o rótulo mostra o sinal de menos ("A−").
const SEXOS = ['Masculino', 'Feminino', 'Não informado'].map(v => ({ valor: v, rotulo: v }))
const TIPOS_SANGUINEOS = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-', 'Não informado']
  .map(v => ({ valor: v, rotulo: v.replace(/-$/, '−') }))

// PacienteRequisicao: nome, dataNascimento (aaaa-mm-dd ou vazio), sexo, tipoSanguineo, alergias, observacoes
const CAMPOS_PACIENTE = [
  { chave: 'nome', rotulo: 'Nome', tipo: 'text', obrigatorio: true },
  { chave: 'dataNascimento', rotulo: 'Data de nascimento', tipo: 'date' },
  { chave: 'sexo', rotulo: 'Sexo', tipo: 'select', opcoes: SEXOS },
  { chave: 'tipoSanguineo', rotulo: 'Tipo sanguíneo', tipo: 'select', opcoes: TIPOS_SANGUINEOS },
  { chave: 'alergias', rotulo: 'Alergias', tipo: 'text' },
  { chave: 'observacoes', rotulo: 'Observações', tipo: 'text' },
]

// "Ativo" vem do back e só muda pelas rotas /inativar e /reativar (métodos do domínio).
const COLUNAS_INSTRUMENTO = [{ rotulo: 'Ativo', valor: i => (i.ativo ? 'Sim' : 'Não') }]
const acaoInstrumento = (item, executar) => item.ativo
  ? <button className="link" onClick={() => executar(() => instrumentos.inativar(item.id))}>Inativar</button>
  : <button className="link" onClick={() => executar(() => instrumentos.reativar(item.id))}>Reativar</button>

export const Instrumentos = () => <Crud titulo="Instrumentos" singular="instrumento" api={instrumentos} campos={CAMPOS_INSTRUMENTO}
  colunasExtras={COLUNAS_INSTRUMENTO} acaoExtra={acaoInstrumento} />
export const Pacientes = () => <Crud titulo="Pacientes" singular="paciente" api={pacientes} campos={CAMPOS_PACIENTE} />
