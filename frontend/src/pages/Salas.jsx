import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { procedimentos, salas } from '../api.js'
import { dataHora } from '../mapeadores.js'

// Linha de um procedimento planejado: iniciar (RN1 + RN7 no back) ou cancelar com motivo.
function Planejado({ p, aoMudar }) {
  const nav = useNavigate()
  const [erro, setErro] = useState('')
  const [cancelando, setCancelando] = useState(false)
  const [motivo, setMotivo] = useState('')

  async function iniciar() {
    setErro('')
    try { await procedimentos.iniciar(p.id); nav(`/operacao/${p.id}`) }
    catch (e) { setErro(e.message) } // ex.: "A Sala 2 já está ocupada pelo procedimento Hernioplastia..."
  }
  async function cancelar(e) {
    e.preventDefault(); setErro('')
    try { await procedimentos.cancelar(p.id, motivo); aoMudar() } catch (err) { setErro(err.message) }
  }

  return (
    <tr>
      <td><b>{p.sala}</b></td><td>{p.tipo}</td><td>{p.paciente}</td><td>{p.cirurgiao}</td>
      <td>
        <div className="linha">
          <button className="btn primario pequeno" onClick={iniciar}>Iniciar</button>
          {!cancelando && <button className="link" onClick={() => setCancelando(true)}>Cancelar</button>}
        </div>
        {cancelando && (
          <form className="linha" onSubmit={cancelar} style={{ marginTop: 8 }}>
            <input required placeholder="Motivo do cancelamento" aria-label={`Motivo do cancelamento de ${p.tipo}`} value={motivo} onChange={e => setMotivo(e.target.value)} />
            <button className="btn claro pequeno">Confirmar cancelamento</button>
            <button type="button" className="link" onClick={() => { setCancelando(false); setMotivo('') }}>Voltar</button>
          </form>
        )}
        {erro && <p className="erro" role="alert">{erro}</p>}
      </td>
    </tr>
  )
}

export default function Salas() {
  const [emUso, setEmUso] = useState([])
  const [planejados, setPlanejados] = useState([])
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(true)

  const carregar = useCallback(async () => {
    try {
      const [u, p] = await Promise.all([salas.emUso(), procedimentos.planejados()])
      setEmUso(u); setPlanejados(p); setErro('')
    } catch (e) { setErro(e.message) }
    setCarregando(false)
  }, [])
  useEffect(() => { carregar() }, [carregar])

  if (carregando) return <p>Carregando…</p>

  return (
    <>
      <header><h1>Salas em uso</h1><Link className="btn primario" to="/novo">Novo procedimento</Link></header>
      {erro && <p className="erro" role="alert">{erro}</p>}

      <h2>Em andamento</h2>
      <table>
        <thead><tr><th>Sala</th><th>Procedimento</th><th>Paciente</th><th>Cirurgião</th><th>Início</th><th></th></tr></thead>
        <tbody>
          {emUso.map(s => (
            <tr key={s.procedimentoId}>
              <td><b>{s.sala}</b> <span className="chip andamento">Ocupada</span></td>
              <td>{s.tipo}</td><td>{s.paciente}</td><td>{s.cirurgiao}</td><td>{dataHora(s.dataHoraInicio)}</td>
              <td><Link className="btn primario pequeno" to={`/operacao/${s.procedimentoId}`}>Abrir operação</Link></td>
            </tr>
          ))}
          {!emUso.length && <tr><td colSpan={6}>Nenhuma sala ocupada no momento.</td></tr>}
        </tbody>
      </table>

      <h2>Planejados</h2>
      <p className="mudo">Dois procedimentos podem ser planejados para a mesma sala; só um pode estar em andamento por vez.</p>
      <table>
        <thead><tr><th>Sala</th><th>Procedimento</th><th>Paciente</th><th>Cirurgião</th><th>Ações</th></tr></thead>
        <tbody>
          {planejados.map(p => <Planejado key={p.id} p={p} aoMudar={carregar} />)}
          {!planejados.length && <tr><td colSpan={5}>Nenhum procedimento planejado.</td></tr>}
        </tbody>
      </table>
    </>
  )
}
