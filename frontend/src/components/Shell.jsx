import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth.jsx'

export default function Shell() {
  const { usuario, sair } = useAuth()
  const link = ({ isActive }) => (isActive ? 'ativo' : '')
  return (
    <div className="adm">
      <aside className="menu">
        <b className="marca">scanOPS</b>
        <NavLink to="/" end className={link}>Salas em uso</NavLink>
        <NavLink to="/novo" className={link}>Novo procedimento</NavLink>
        <NavLink to="/historico" className={link}>Histórico</NavLink>
        <NavLink to="/instrumentos" className={link}>Instrumentos</NavLink>
        <NavLink to="/pacientes" className={link}>Pacientes</NavLink>
        <div className="menu-rodape">
          <span>{usuario.nome ?? usuario.email}</span>
          <button className="link claro" onClick={sair}>Sair</button>
        </div>
      </aside>
      <main className="conteudo"><Outlet /></main>
    </div>
  )
}
