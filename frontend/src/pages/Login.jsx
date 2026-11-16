import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { login } from '../api.js'
import { useAuth } from '../auth.jsx'

export default function Login() {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState('')
  const [enviando, setEnviando] = useState(false)
  const { entrar } = useAuth()
  const nav = useNavigate()

  async function enviar(e) {
    e.preventDefault(); setEnviando(true); setErro('')
    try { entrar(await login(email, senha)); nav('/') } catch (err) { setErro(err.message) }
    setEnviando(false)
  }

  return (
    <div className="login">
      <form className="login-card" onSubmit={enviar}>
        <h1>scanOPS</h1>
        <p>Controle de instrumentos cirúrgicos</p>
        <label>E-mail<input type="email" value={email} onChange={e => setEmail(e.target.value)} required autoFocus /></label>
        <label>Senha<input type="password" value={senha} onChange={e => setSenha(e.target.value)} required /></label>
        {erro && <p className="erro" role="alert">{erro}</p>}
        <button className="btn primario" disabled={enviando}>{enviando ? 'Entrando…' : 'Entrar'}</button>
      </form>
    </div>
  )
}
