import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/customer-api': {
        target: 'http://localhost:5101',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/customer-api/, ''),
      },
      '/account-api': {
        target: 'http://localhost:5102',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/account-api/, ''),
      },
      '/deposit-api': {
        target: 'http://localhost:5103',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/deposit-api/, ''),
      },
      '/audit-api': {
        target: 'http://localhost:5104',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/audit-api/, ''),
      },
    },
  },
})
