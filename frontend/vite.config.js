import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// O back libera CORS para localhost:5173 e 5174, então não precisa de proxy.
export default defineConfig({ plugins: [react()], server: { port: 5173 } })
