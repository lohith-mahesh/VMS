interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_AUTH_MODE?: 'development' | 'entra';
  readonly VITE_ENTRA_CLIENT_ID?: string;
  readonly VITE_ENTRA_TENANT_ID?: string;
  readonly VITE_API_SCOPE?: string;
  readonly VITE_DEV_ROLE?: 'HostRequester' | 'ExportControl' | 'Reception';
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
