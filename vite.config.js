import { resolve } from 'path'
import { compression } from 'vite-plugin-compression2'
import { defineConfig } from 'vite'

export default defineConfig({
    plugins: [
        // ...your plugin
        compression({ algorithm: 'gzip', threshold: 10240 }),
    ],
    build: {
        rollupOptions: {
            input: {
                main: resolve(__dirname, 'index.html'),
                nested: resolve(__dirname, './CoreAPI/wwwroot/index.html'),
            },
        },
    },
})