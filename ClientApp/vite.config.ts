import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { rm } from 'node:fs/promises';
import path from 'node:path';

function cleanGeneratedAssets() {
    return {
        name: 'clean-generated-assets',
        async buildStart() {
            const assetsPath = path.resolve(__dirname, '../wwwroot/assets');
            await rm(assetsPath, { recursive: true, force: true });
        },
    };
}

// https://vite.dev/config/
export default defineConfig({
    plugins: [cleanGeneratedAssets(), tailwindcss(), react()],
    base: '/',
    build: {
        outDir: '../wwwroot',
        emptyOutDir: false,
        rollupOptions: {
            output: {
                manualChunks: undefined
            }
        }
    }
});
