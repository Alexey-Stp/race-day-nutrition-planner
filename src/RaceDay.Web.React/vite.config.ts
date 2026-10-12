import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5208',
        changeOrigin: true,
      }
    }
  },
  preview: {
    port: 4173,
    proxy: {
      // CI runs the API with ASPNETCORE_URLS=http://127.0.0.1:8080. Use the IPv4
      // literal so Node does not resolve `localhost` to ::1 where nothing listens.
      '/api': {
        target: 'http://127.0.0.1:8080',
        changeOrigin: true,
      }
    }
  }
})
