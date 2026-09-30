import { accessToken, developmentRole, isDevelopmentAuthentication } from '../auth/auth';

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly errors: Record<string, string[]> = {}
  ) {
    super(message);
  }
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (!(init.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  headers.set('Accept', 'application/json');
  const token = await accessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (isDevelopmentAuthentication) {
    headers.set('X-Dev-Role', developmentRole());
    headers.set('X-Dev-User-Id', '00000000-0000-0000-0000-000000000001');
    headers.set('X-Dev-User', 'Development User');
    headers.set('X-Dev-Email', 'developer@example.test');
  }

  const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
  if (!response.ok) throw await toApiError(response);
  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}

export async function download(path: string, fallbackName: string): Promise<void> {
  const headers = new Headers();
  const token = await accessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (isDevelopmentAuthentication) headers.set('X-Dev-Role', developmentRole());
  const response = await fetch(`${baseUrl}${path}`, { headers });
  if (!response.ok) throw await toApiError(response);
  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8''|\")?([^";]+)/i.exec(disposition);
  const fileName = match?.[1] ? decodeURIComponent(match[1].replace(/"$/, '')) : fallbackName;
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function queryString(values: Record<string, string | number | undefined>): string {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== '') params.set(key, String(value));
  });
  const query = params.toString();
  return query ? `?${query}` : '';
}

async function toApiError(response: Response): Promise<ApiError> {
  const type = response.headers.get('Content-Type') ?? '';
  if (!type.includes('application/problem+json') && !type.includes('application/json')) {
    return new ApiError(`Request failed with status ${response.status}.`, response.status);
  }

  const problem = await response.json() as { title?: string; errors?: Record<string, string[]> };
  return new ApiError(problem.title ?? `Request failed with status ${response.status}.`, response.status, problem.errors);
}
