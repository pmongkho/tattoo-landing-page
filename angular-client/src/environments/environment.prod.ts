export const environment = {
  production: true,
  apiBaseUrl: (globalThis as { __API_BASE_URL__?: string }).__API_BASE_URL__ ?? ''
};
