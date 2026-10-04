import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
export default defineConfig({
    base: '/react/',
    plugins: [react()],
    server: { port: 5173, proxy: { '/api': 'http://127.0.0.1:5188', '/print': 'http://127.0.0.1:5188', '/assets': 'http://127.0.0.1:5188' } },
    build: { outDir: 'dist', emptyOutDir: true }
});
