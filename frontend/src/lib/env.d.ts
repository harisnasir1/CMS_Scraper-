interface ImportMetaEnv {
    readonly VITE_API_URL: string
    // add other env vars here if you have them
  }
  
  interface ImportMeta {
    readonly env: ImportMetaEnv
  }