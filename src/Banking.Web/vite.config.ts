import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/customer-api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/account-api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/deposit-api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/audit-api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/gateway-api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/gateway-api/, ''),
      },
    },
  },
})
