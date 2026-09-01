import { defineConfig } from 'vite'
import path from 'path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'

/**
 * The admin application. It builds and serves independently of the storefront and only shares
 * the API client and design tokens under src/shared. Port 5199 is already allowed by every
 * service's CORS policy.
 */
export default defineConfig({
  root: path.resolve(__dirname, 'admin'),
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5199,
    strictPort: true,
    fs: {
      // The entry lives in admin/ but imports shared code from src/.
      allow: [path.resolve(__dirname)],
    },
  },
  build: {
    outDir: path.resolve(__dirname, 'dist-admin'),
    emptyOutDir: true,
  },
})
