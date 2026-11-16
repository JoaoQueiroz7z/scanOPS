import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth.jsx'
import Shell from './components/Shell.jsx'
import Login from './pages/Login.jsx'
import Atual from './pages/Atual.jsx'
import NovoProcedimento from './pages/NovoProcedimento.jsx'
import Operacao from './pages/Operacao.jsx'
import { Instrumentos, Pacientes } from './pages/Cadastros.jsx'

function Privado({ children }) {
  const { usuario } = useAuth()
  return usuario ? children : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/operacao" element={<Privado><Operacao /></Privado>} />
      <Route element={<Privado><Shell /></Privado>}>
        <Route index element={<Atual />} />
        <Route path="novo" element={<NovoProcedimento />} />
        <Route path="instrumentos" element={<Instrumentos />} />
        <Route path="pacientes" element={<Pacientes />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
