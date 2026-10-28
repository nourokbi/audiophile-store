import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // The browser only ever talks to this dev server, which forwards to the .NET API:
    //   /api/products            → http://localhost:5040/products   (the shop's API calls)
    //   /payments/fawaterak/...  → http://localhost:5040/payments/... (webhook + return redirect)
    // So one public ngrok URL for this server (`ngrok http 5173`) serves the shop AND lets
    // Fawaterak reach the webhook. One origin also means the API needs no CORS setup.
    proxy: {
      '/api': {
        target: 'http://localhost:5040',
        rewrite: (path) => path.replace(/^\/api/, ''),
        // The API scopes the refresh cookie to /auth, the browser sees that path as /api/auth
        cookiePathRewrite: { '/auth': '/api/auth' },
      },
      '/payments': {
        target: 'http://localhost:5040',
      },
    },
  },
})
