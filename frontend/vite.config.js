import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [
  ],
  server: {
    host: true, // Allows the server to be accessed externally
    strictPort: false // Disables strict host checking
  }
});
