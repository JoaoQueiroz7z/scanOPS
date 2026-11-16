import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { instrumentos, pacientes, procedimentos } from '../api.js'
import { useAuth } from '../auth.jsx'

export default function NovoProcedimento() {
  const { usuario } = useAuth()
  const nav = useNavigate()
  const [pacs, setPacs] = useState([])
  const [insts, setInsts] = useState([])
  const [f, setF] = useState({ tipo: '', sala: '', descricao: '', pacienteId: '', instrumentistaId: usuario.id ?? '' })
  const [itens, setItens] = useState([]) // [{instrumentoId, quantidadePrevista}]
  const [erro, setErro] = useState('')
  const [criado, setCriado] = useState(null) // procedimento recém-planejado (ProcedimentoResposta)
  const set = k => e => setF({ ...f, [k]: e.target.value })

  useEffect(() => {
    pacientes.listar().then(setPacs).catch(e => setErro(e.message))
    instrumentos.listar().then(setInsts).catch(e => setErro(e.message))
  }, [])

  const nomeInst = id => insts.find(i => i.id === id)?.nome ?? id
  const disponiveis = insts.filter(i => !itens.some(x => x.instrumentoId === i.id))

  async function criar(e) {
    e.preventDefault(); setErro('')
    if (!itens.length) return setErro('Adicione pelo menos um instrumento previsto.')
    try {
      // Não existe GET /api/usuarios no back ainda: o cirurgião é quem está logado.
      setCriado(await procedimentos.criar({ ...f, cirurgiaoId: usuario.id, itens }))
    } catch (err) { setErro(err.message) }
  }

  // O "procedimento atual" do back é o de início mais recente. Um procedimento só planejado
  // ainda não começou, então ele é iniciado aqui mesmo, pelo id devolvido na criação.
  async function iniciar() {
    setErro('')
    try { await procedimentos.iniciar(criado.id); nav('/') } catch (err) { setErro(err.message) }
  }

  if (criado) return (
    <>
      <header><h1>Novo procedimento</h1></header>
      <div className="cartao vazio">
        <h2>Procedimento planejado</h2>
        <p>{criado.tipo} · {criado.itens.length} {criado.itens.length === 1 ? 'instrumento previsto' : 'instrumentos previstos'}.</p>
        <p className="mudo">Ao iniciar, ele passa a ser o procedimento atual e a contagem pode começar.</p>
        {erro && <p className="erro" role="alert">{erro}</p>}
        <div className="linha">
          <button className="btn primario" onClick={iniciar}>Iniciar agora</button>
          <button className="btn claro" onClick={() => { setCriado(null); setItens([]); setF({ ...f, tipo: '', sala: '', descricao: '', pacienteId: '' }) }}>Planejar outro</button>
        </div>
      </div>
    </>
  )

  return (
    <>
      <header><h1>Novo procedimento</h1></header>
      <form className="cartao form" onSubmit={criar}>
        <div className="grade">
          <label>Tipo<input required value={f.tipo} onChange={set('tipo')} placeholder="Ex.: Apendicectomia" /></label>
          <label>Sala<input required value={f.sala} onChange={set('sala')} placeholder="Ex.: 03" /></label>
          <label>Paciente
            <select required value={f.pacienteId} onChange={set('pacienteId')}>
              <option value="">Selecione…</option>
              {pacs.map(p => <option key={p.id} value={p.id}>{p.nome}</option>)}
            </select>
          </label>
          <label>ID do instrumentista<input required value={f.instrumentistaId} onChange={set('instrumentistaId')} /></label>
        </div>
        <label>Descrição<input value={f.descricao} onChange={set('descricao')} /></label>

        <h2>Instrumentos previstos</h2>
        <select value="" onChange={e => e.target.value && setItens([...itens, { instrumentoId: e.target.value, quantidadePrevista: 1 }])} aria-label="Adicionar instrumento">
          <option value="">Adicionar instrumento…</option>
          {disponiveis.map(i => <option key={i.id} value={i.id}>{i.nome}</option>)}
        </select>
        {itens.map((it, idx) => (
          <div className="linha item-prev" key={it.instrumentoId}>
            <span>{nomeInst(it.instrumentoId)}</span>
            <input type="number" min="1" aria-label="Quantidade prevista" value={it.quantidadePrevista}
              onChange={e => setItens(itens.map((x, j) => j === idx ? { ...x, quantidadePrevista: Number(e.target.value) } : x))} />
            <button type="button" className="link vermelho" onClick={() => setItens(itens.filter((_, j) => j !== idx))}>Remover</button>
          </div>
        ))}
        {erro && <p className="erro" role="alert">{erro}</p>}
        <div className="linha"><button className="btn primario">Planejar procedimento</button></div>
      </form>
    </>
  )
}
