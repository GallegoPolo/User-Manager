import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api/auth': {
        target: 'http://localhost:5149',
        changeOrigin: true,
      },
      '/api/users': {
        target: 'http://localhost:5255',
        changeOrigin: true,
      },
      '/api/Audit': {
        target: 'http://localhost:7175',
        changeOrigin: true,
      },
    },
  },
})