import { rmSync } from 'node:fs';
import { resolve } from 'node:path';
import { loadEnv, type Plugin } from 'vite';
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// public/mockServiceWorker.js luôn bị copy vào dist; bản build không giả lập thì xoá đi.
function dropMockWorker(mode: string): Plugin {
  let outDir = 'dist';
  return {
    name: 'drop-mock-worker',
    apply: 'build',
    configResolved(c) {
      outDir = resolve(c.root, c.build.outDir);
    },
    closeBundle() {
      if (loadEnv(mode, process.cwd(), '').VITE_USE_MOCK !== 'true') {
        rmSync(resolve(outDir, 'mockServiceWorker.js'), { force: true });
      }
    },
  };
}

export default defineConfig(({ mode }) => ({
  plugins: [react(), dropMockWorker(mode)],
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
}));
