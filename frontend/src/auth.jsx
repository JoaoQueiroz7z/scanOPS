import { createContext, useContext, useState } from 'react'

const Ctx = createContext(null)
const ler = () => { try { return JSON.parse(sessionStorage.getItem('usuario')) } catch { return null } }

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(ler)
  const entrar = u => { sessionStorage.setItem('usuario', JSON.stringify(u)); setUsuario(u) }
  const sair = () => { sessionStorage.removeItem('usuario'); setUsuario(null) }
  return <Ctx.Provider value={{ usuario, entrar, sair }}>{children}</Ctx.Provider>
}
export const useAuth = () => useContext(Ctx)
