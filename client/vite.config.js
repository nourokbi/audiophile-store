import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // In development, /api/products is forwarded to the .NET API as /products,
    // so the browser sees one origin and no CORS setup is needed.
    proxy: {
      '/api': {
        target: 'http://localhost:5040',
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
