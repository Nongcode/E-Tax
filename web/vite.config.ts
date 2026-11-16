import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id: string) {
          if (!id.includes('node_modules/')) return undefined;
          if (/node_modules\/(react|react-dom|react-router|react-router-dom|scheduler)\//.test(id)) return 'react';
          if (id.includes('node_modules/@ant-design/icons')) return 'antd-icons';
          if (/node_modules\/(@ant-design|@rc-component|rc-[^/]+)\//.test(id)) return 'antd-rc';
          if (id.includes('node_modules/antd/')) return 'antd';
          return 'vendor';
        },
      },
    },
  },
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
