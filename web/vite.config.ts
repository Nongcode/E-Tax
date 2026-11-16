import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['src/test/setup.ts'],
    globals: true,
    css: false,
    env: { VITE_API_URL: 'http://localhost', VITE_USE_MOCK: 'false' },
    include: ['src/**/*.test.{ts,tsx}'],
  },
});
