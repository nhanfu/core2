import { defineConfig } from 'vite'
import dotenv from 'dotenv'

// Load environment variables from a `.env` file
dotenv.config()

export default defineConfig({
  plugins: [
  ],
  server: {
    host: true, // Allows the server to be accessed externally
    strictPort: false, // Disables strict host checking,
    allowedHosts: process.env.ALLOWED_HOSTS ? process.env.ALLOWED_HOSTS.split(',') : []
  }
});