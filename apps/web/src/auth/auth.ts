import { PublicClientApplication, type AccountInfo, type Configuration } from '@azure/msal-browser';
import type { UserRole } from '../lib/types';

const mode = import.meta.env.VITE_AUTH_MODE ?? 'development';
let application: PublicClientApplication | undefined;

export async function initializeAuthentication(): Promise<void> {
  if (mode === 'development') return;
  const clientId = required('VITE_ENTRA_CLIENT_ID', import.meta.env.VITE_ENTRA_CLIENT_ID);
  const tenantId = required('VITE_ENTRA_TENANT_ID', import.meta.env.VITE_ENTRA_TENANT_ID);
  const configuration: Configuration = {
    auth: {
      clientId,
      authority: `https://login.microsoftonline.com/${tenantId}`,
      redirectUri: window.location.origin,
      postLogoutRedirectUri: window.location.origin
    },
    cache: {
      cacheLocation: 'sessionStorage'
    }
  };
  application = new PublicClientApplication(configuration);
  await application.initialize();
  const response = await application.handleRedirectPromise();
  if (response?.account) application.setActiveAccount(response.account);
  if (!activeAccount()) {
    await application.loginRedirect({ scopes: [required('VITE_API_SCOPE', import.meta.env.VITE_API_SCOPE)] });
  }
}

export async function accessToken(): Promise<string | undefined> {
  if (mode === 'development') return undefined;
  const account = activeAccount();
  if (!application || !account) throw new Error('Microsoft sign-in is not initialized.');
  const scopes = [required('VITE_API_SCOPE', import.meta.env.VITE_API_SCOPE)];
  try {
    return (await application.acquireTokenSilent({ account, scopes })).accessToken;
  } catch {
    await application.acquireTokenRedirect({ account, scopes });
    return undefined;
  }
}

export async function signOut(): Promise<void> {
  if (mode === 'development') {
    sessionStorage.clear();
    window.location.assign('/');
    return;
  }

  await application?.logoutRedirect({ account: activeAccount() });
}

export function developmentRole(): UserRole {
  const saved = sessionStorage.getItem('rrvms-dev-role');
  if (saved === 'HostRequester' || saved === 'ExportControl' || saved === 'Reception') return saved;
  return import.meta.env.VITE_DEV_ROLE ?? 'HostRequester';
}

export function setDevelopmentRole(role: UserRole): void {
  sessionStorage.setItem('rrvms-dev-role', role);
  window.location.reload();
}

export const isDevelopmentAuthentication = mode === 'development';

function activeAccount(): AccountInfo | undefined {
  if (!application) return undefined;
  return application.getActiveAccount() ?? application.getAllAccounts()[0];
}

function required(name: string, value: string | undefined): string {
  if (!value) throw new Error(`${name} must be configured.`);
  return value;
}
