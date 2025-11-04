import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
        outDir: '../app/wwwroot/', // This is the default, but you can change it
        // For example, to output to a 'public/build' directory:
        // outDir: 'public/build',
      },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5292',
        changeOrigin: true,
        secure: false
      }
    }
  }
})