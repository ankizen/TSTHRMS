/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the deployed API, e.g. "https://api.example.com/api" - falls back to the
   * relative "/api" (same-origin, dev proxy or single-origin deploy) when unset. */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
