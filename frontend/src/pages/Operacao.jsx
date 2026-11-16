import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import Camera from '../components/Camera.jsx'
import { procedimentos } from '../api.js'
import { normalizarProcedimento, rotuloSala, statusDe } from '../mapeadores.js'

function ItemLinha({ item, executar }) {
  const [qE, setQE] = useState(1)
  const [qS, setQS] = useState(1)
  const [peso, setPeso] = useState('')
  const [camera, setCamera] = useState(false)
  const [pesando, setPesando] = useState(false)
  const aberto = item.entrada - item.saida

  async function pesar() {
    setPesando(true)
    // PesagemResposta: { pesoMedido }. O back já grava esse peso no item.
    await executar(async () => { const r = await procedimentos.pesar(item.id); setPeso(String(r?.pesoMedido ?? '')) }, false)
    setPesando(false)
  }
  return (
    <tr>
      <td><b>{item.nome}</b></td>
      <td>{item.previsto}</td><td>{item.entrada}</td><td>{item.saida}</td>
      <td className={aberto > 0 ? 'aviso' : 'ok'}>{aberto}</td>
      <td>
        <form className="mini" onSubmit={e => { e.preventDefault(); executar(() => procedimentos.entrada(item.id, Number(qE))) }}>
          <input type="number" min="1" aria-label={`Quantidade de entrada de ${item.nome}`} value={qE} onChange={e => setQE(e.target.value)} />
          <button className="btn pequeno">Registrar entrada</button>
        </form>
        {/* RN5: saída >= 0 (zero = nenhuma unidade voltou). O peso é obrigatório: o back confere o peso (RN4). */}
        <form className="mini" onSubmit={e => { e.preventDefault(); executar(() => procedimentos.saida(item.id, { quantidade: Number(qS), pesoMedido: Number(peso), identificadoPorCamera: camera })) }}>
          <input type="number" min="0" aria-label={`Quantidade de saída de ${item.nome}`} value={qS} onChange={e => setQS(e.target.value)} />
          <input type="number" step="any" min="0" required placeholder="Peso (g)" title="Informe o peso ou use Pesar" aria-label={`Peso medido de ${item.nome}`} value={peso} onChange={e => setPeso(e.target.value)} />
          <button type="button" className="btn pequeno claro" onClick={pesar} disabled={pesando}>{pesando ? 'Pesando…' : 'Pesar'}</button>
          <label className="check"><input type="checkbox" checked={camera} onChange={e => setCamera(e.target.checked)} />Câmera</label>
          <button className="btn pequeno">Registrar saída</button>
        </form>
      </td>
    </tr>
  )
}

export default function Operacao() {
  const nav = useNavigate()
  const [p, setP] = useState(null)
  const [erro, setErro] = useState('')
  const [justificativa, setJustificativa] = useState('')
  const [final, setFinal] = useState('')

  const carregar = useCallback(async () => {
    try { setP(normalizarProcedimento(await procedimentos.atual())); setErro('') } catch (e) { setErro(e.message) }
  }, [])
  useEffect(() => { carregar() }, [carregar])

  // Executa uma ação, mostra o erro da regra de negócio (se houver) e recarrega o procedimento.
  async function executar(fn, recarregar = true) {
    setErro('')
    try { await fn(); if (recarregar) await carregar() } catch (e) { setErro(e.message) }
  }
  async function finalizar() {
    setErro('')
    try { const r = await procedimentos.finalizar(justificativa.trim() || null); setFinal(r?.status ?? 'Finalizado'); await carregar() } catch (e) { setErro(e.message) }
  }

  if (!p) return <div className="op"><div className="op-topo"><Link className="link claro" to="/">Voltar</Link></div>{erro ? <p className="erro pad" role="alert">{erro}</p> : <p className="pad">Carregando…</p>}</div>

  const emCampo = p.itens.reduce((s, i) => s + i.entrada, 0)
  const devolvidos = p.itens.reduce((s, i) => s + i.saida, 0)
  const faltando = emCampo - devolvidos
  const st = statusDe(p.status)

  return (
    <div className="op">
      <header className="op-topo">
        <Link className="link claro" to="/">Voltar</Link>
        <b>scanOPS</b>
        <span>{p.tipo} · {rotuloSala(p.sala)} · {p.paciente} · {p.cirurgiao}</span>
      </header>
      <div className="op-corpo">
        <aside className="lateral"><Camera /></aside>
        <main className="conferencia">
          <div className={`alerta ${faltando > 0 ? 'erro' : 'ok'}`} role="status">
            <b>{faltando > 0 ? faltando : '✓'}</b>
            <div>
              <h2>{faltando > 0 ? `${faltando} ${faltando === 1 ? 'instrumento ainda não retornou' : 'instrumentos ainda não retornaram'}` : emCampo ? 'Todos os instrumentos retornaram' : 'Nenhum instrumento em campo'}</h2>
              <p>{emCampo} em campo · {devolvidos} devolvidos</p>
            </div>
          </div>
          {erro && <p className="erro caixa-erro" role="alert">{erro}</p>}
          <div className="tabela-rolavel">
            <table className="tabela-conf">
              <thead><tr><th>Instrumento</th><th>Previsto</th><th>Em campo</th><th>Devolvidos</th><th>Em aberto</th><th>Registrar</th></tr></thead>
              <tbody>{p.itens.map(i => <ItemLinha key={i.id} item={i} executar={executar} />)}</tbody>
            </table>
          </div>
          {st === 'andamento' && (
            <div className="finalizar">
              {/* Sempre visível: o back (RN3) exige justificativa se houver divergência de contagem OU de peso (RN4),
                  e a tela não tem como saber a divergência de peso antes de tentar finalizar. */}
              <textarea aria-label="Justificativa" placeholder="Justificativa (obrigatória se houver divergência de contagem ou de peso)" value={justificativa} onChange={e => setJustificativa(e.target.value)} />
              <button className={`btn ${faltando > 0 ? 'perigo' : 'primario'}`} onClick={finalizar}>Finalizar procedimento</button>
            </div>
          )}
          {st !== 'andamento' && <p className="mudo">Procedimento {p.status}. {final && `Resultado: ${final}.`}</p>}
        </main>
      </div>
    </div>
  )
}
