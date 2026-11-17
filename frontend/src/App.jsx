import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth.jsx'
import Shell from './components/Shell.jsx'
import Login from './pages/Login.jsx'
import Salas from './pages/Salas.jsx'
import NovoProcedimento from './pages/NovoProcedimento.jsx'
import Operacao from './pages/Operacao.jsx'
import { Historico, HistoricoDetalhe } from './pages/Historico.jsx'
import { Instrumentos, Pacientes } from './pages/Cadastros.jsx'

function Privado({ children }) {
  const { usuario } = useAuth()
  return usuario ? children : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      {/* Cada procedimento em andamento é operado pelo seu id (uma sala por procedimento, RN7). */}
      <Route path="/operacao/:id" element={<Privado><Operacao /></Privado>} />
      <Route element={<Privado><Shell /></Privado>}>
        <Route index element={<Salas />} />
        <Route path="novo" element={<NovoProcedimento />} />
        <Route path="historico" element={<Historico />} />
        <Route path="historico/:id" element={<HistoricoDetalhe />} />
        <Route path="instrumentos" element={<Instrumentos />} />
        <Route path="pacientes" element={<Pacientes />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
