import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { procedimentos } from '../api.js'
import { normalizarProcedimento, statusDe } from '../mapeadores.js'

const ROTULO = { planejado: 'Planejado', andamento: 'Em andamento', finalizado: 'Finalizado', cancelado: 'Cancelado', outro: '' }

export default function Atual() {
  const [p, setP] = useState(null)
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState('')
  const [motivo, setMotivo] = useState('')

  const carregar = useCallback(async () => {
    setCarregando(true)
    try { setP(normalizarProcedimento(await procedimentos.atual())); setErro('') }
    catch (e) { setP(null); if (e.status !== 404) setErro(e.message) }
    setCarregando(false)
  }, [])
  useEffect(() => { carregar() }, [carregar])

  async function agir(fn) { setErro(''); try { await fn(); await carregar() } catch (e) { setErro(e.message) } }

  if (carregando) return <p>Carregando…</p>
  if (!p) return (
    <>
      <header><h1>Procedimento atual</h1></header>
      {erro && <p className="erro" role="alert">{erro}</p>}
      <div className="cartao vazio"><p>Nenhum procedimento cadastrado.</p><Link className="btn primario" to="/novo">Planejar procedimento</Link></div>
    </>
  )

  const st = statusDe(p.status)
  return (
    <>
      <header><h1>Procedimento atual</h1><span className={`chip ${st}`}>{ROTULO[st] || p.status}</span></header>
      {erro && <p className="erro" role="alert">{erro}</p>}
      <div className="cartao">
        <dl className="dados">
          <div><dt>Tipo</dt><dd>{p.tipo || '—'}</dd></div><div><dt>Sala</dt><dd>{p.sala || '—'}</dd></div>
          <div><dt>Paciente</dt><dd>{p.paciente}</dd></div><div><dt>Cirurgião</dt><dd>{p.cirurgiao}</dd></div>
        </dl>
        <table>
          <thead><tr><th>Instrumento</th><th>Previsto</th><th>Em campo</th><th>Devolvidos</th></tr></thead>
          <tbody>{p.itens.map(i => <tr key={i.id}><td>{i.nome}</td><td>{i.previsto}</td><td>{i.entrada}</td><td>{i.saida}</td></tr>)}</tbody>
        </table>
        <div className="linha acoes-base">
          {st === 'planejado' && <button className="btn primario" onClick={() => agir(() => procedimentos.iniciar(p.id))}>Iniciar procedimento</button>}
          {st === 'andamento' && <Link className="btn primario" to="/operacao">Abrir operação</Link>}
          {(st === 'planejado' || st === 'andamento') && (
            <form className="linha" onSubmit={e => { e.preventDefault(); agir(() => procedimentos.cancelar(p.id, motivo)) }}>
              <input required placeholder="Motivo do cancelamento" value={motivo} onChange={e => setMotivo(e.target.value)} />
              <button className="btn claro">Cancelar procedimento</button>
            </form>
          )}
          <Link className="btn claro" to="/novo">Novo procedimento</Link>
        </div>
      </div>
    </>
  )
}
