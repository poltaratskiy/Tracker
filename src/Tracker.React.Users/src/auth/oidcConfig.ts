import type { UserManagerSettings } from 'oidc-client-ts';
import { Log } from 'oidc-client-ts';

Log.setLogger(console);
Log.setLevel(Log.DEBUG);

export const oidcConfig: UserManagerSettings = {
  authority: import.meta.env.VITE_OIDC_AUTHORITY!,
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID!,
  redirect_uri: import.meta.env.VITE_OIDC_REDIRECT_URI!,
  response_type: 'code',
  scope: import.meta.env.VITE_OIDC_SCOPE || 'openid',
  // Currently is more simple: without silent renew
  automaticSilentRenew: import.meta.env.VITE_OIDC_SILENT_RENEW!,
  loadUserInfo: false,
  monitorSession: true,
  silent_redirect_uri: import.meta.env.VITE_OIDC_SILENT_REDIRECT_URI!,
  // Storage by default — localStorage. If you want to 
  // almost always would redirect to SSO and nothing was here — later can make it in-memory.
};

