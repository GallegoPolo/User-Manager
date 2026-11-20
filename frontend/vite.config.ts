import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

const bff = { target: 'http://localhost:5080', changeOrigin: false }

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': bff,
      '/bff': bff,
      '/signin-oidc': bff,
      '/signout-callback-oidc': bff,
    },
  },
  test: {
    environment: 'jsdom',
  },
})
