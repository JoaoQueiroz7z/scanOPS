import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { procedimentos } from '../api.js'
import { dataHora, duracao, normalizarProcedimento, ROTULO_STATUS, statusDe } from '../mapeadores.js'

// Histórico de cirurgias encerradas (Finalizadas e Canceladas). Só leitura: sem editar nem excluir.
export function Historico() {
  const [lista, setLista] = useState(null)
  const [erro, setErro] = useState('')

  useEffect(() => { procedimentos.historico().then(setLista).catch(e => setErro(e.message)) }, [])

  if (!lista && !erro) return <p>Carregando…</p>
  return (
    <>
      <header><h1>Histórico</h1></header>
      {erro && <p className="erro" role="alert">{erro}</p>}
      <table>
        <thead><tr><th>Data</th><th>Procedimento</th><th>Sala</th><th>Paciente</th><th>Cirurgião</th><th>Duração</th><th>Status</th><th></th></tr></thead>
        <tbody>
          {(lista ?? []).map(h => (
            <tr key={h.id}>
              <td>{dataHora(h.dataHoraFim)}</td>
              <td>{h.tipo}</td><td>{h.sala}</td><td>{h.paciente}</td><td>{h.cirurgiao}</td>
              <td>{duracao(h.duracaoMinutos)}</td>
              <td>
                <span className={`chip ${statusDe(h.status)}`}>{ROTULO_STATUS[h.status] ?? h.status}</span>
                {h.teveDivergencia && <span className="mudo"> · com justificativa</span>}
              </td>
              <td><Link className="link" to={`/historico/${h.id}`}>Ver detalhes</Link></td>
            </tr>
          ))}
          {lista && !lista.length && <tr><td colSpan={8}>Nenhuma cirurgia encerrada ainda.</td></tr>}
        </tbody>
      </table>
    </>
  )
}

const gramas = v => `${Number(v ?? 0).toLocaleString('pt-BR')} g`

// Detalhe completo de um procedimento do histórico.
export function HistoricoDetalhe() {
  const { id } = useParams()
  const [p, setP] = useState(null)
  const [erro, setErro] = useState('')

  useEffect(() => {
    procedimentos.obter(id).then(r => setP(normalizarProcedimento(r))).catch(e => setErro(e.message))
  }, [id])

  if (erro) return <><header><h1>Histórico</h1></header><p className="erro" role="alert">{erro}</p><Link className="link" to="/historico">Voltar ao histórico</Link></>
  if (!p) return <p>Carregando…</p>

  return (
    <>
      <header>
        <h1>{p.tipo}</h1>
        <span className={`chip ${statusDe(p.status)}`}>{ROTULO_STATUS[p.status] ?? p.status}</span>
      </header>
      <Link className="link" to="/historico">← Voltar ao histórico</Link>
      <div className="cartao">
        <dl className="dados">
          <div><dt>Sala</dt><dd>{p.sala}</dd></div>
          <div><dt>Paciente</dt><dd>{p.paciente}</dd></div>
          <div><dt>Cirurgião</dt><dd>{p.cirurgiao}</dd></div>
          <div><dt>Instrumentista</dt><dd>{p.instrumentista}</dd></div>
          <div><dt>Início</dt><dd>{dataHora(p.inicio)}</dd></div>
          <div><dt>{p.status === 'Cancelado' ? 'Cancelado em' : 'Fim'}</dt><dd>{dataHora(p.fim)}</dd></div>
          <div><dt>Duração</dt><dd>{duracao(p.duracaoMinutos)}</dd></div>
          <div><dt>Descrição</dt><dd>{p.descricao || '—'}</dd></div>
        </dl>
        {p.justificativa && <p><b>Justificativa da finalização:</b> {p.justificativa}</p>}
        {p.motivoCancelamento && <p><b>Motivo do cancelamento:</b> {p.motivoCancelamento}</p>}
        <h2>Itens</h2>
        <table>
          <thead><tr><th>Instrumento</th><th>Previsto</th><th>Inicial</th><th>Final</th><th>Peso medido</th><th>Peso esperado</th><th>Status</th></tr></thead>
          <tbody>
            {p.itens.map(i => (
              <tr key={i.id}>
                <td>{i.nome}</td><td>{i.previsto}</td><td>{i.entrada}</td><td>{i.saida}</td>
                <td>{gramas(i.pesoMedido)}</td><td>{gramas(i.pesoEsperado)}</td>
                <td><span className={`chip ${i.status === 'Divergente' ? 'cancelado' : i.status === 'Conferido' ? 'finalizado' : ''}`}>{i.status}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
        <p className="mudo">Os nomes e pesos de referência dos itens são os da época do procedimento.</p>
      </div>
    </>
  )
}
