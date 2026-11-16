/// <reference types="vite/client" />

interface ImportMetaEnv {
  VITE_USE_MOCK: string;
  VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
