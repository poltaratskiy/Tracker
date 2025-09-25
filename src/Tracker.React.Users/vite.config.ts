import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,       // 0.0.0.0
    port: 5000, // external docker port
    strictPort: true,
    hmr: { clientPort: 5000 }, // to HMR worked with proxy (internal docker port)
  },
})
