import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: { chunkSizeWarningLimit: 1200 },
  test: {
    environment: 'jsdom',
    setupFiles: ['src/test/setup.ts'],
    globals: true,
    testTimeout: 15000,
    css: false,
    env: { VITE_API_URL: 'http://localhost', VITE_USE_MOCK: 'false' },
    include: ['src/**/*.test.{ts,tsx}'],
  },
});
