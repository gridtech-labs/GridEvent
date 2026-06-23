import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5001';

// Helper: read the persisted access token written by zustand-persist (key = "gt-auth")
function getPersistedToken(): string | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem('gt-auth');
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { state?: { accessToken?: string | null } };
    return parsed?.state?.accessToken ?? null;
  } catch {
    return null;
  }
}

// Helper: overwrite only the accessToken inside the persisted store without
// triggering a full Zustand re-render (the interceptor runs outside React).
function setPersistedToken(token: string): void {
  if (typeof window === 'undefined') return;
  try {
    const raw = localStorage.getItem('gt-auth');
    const parsed = raw ? (JSON.parse(raw) as { state?: Record<string, unknown>; version?: number }) : { state: {} };
    parsed.state = { ...(parsed.state ?? {}), accessToken: token, isAuthenticated: true };
    localStorage.setItem('gt-auth', JSON.stringify(parsed));
  } catch {
    // ignore
  }
}

function clearPersistedToken(): void {
  if (typeof window === 'undefined') return;
  try {
    const raw = localStorage.getItem('gt-auth');
    if (!raw) return;
    const parsed = JSON.parse(raw) as { state?: Record<string, unknown>; version?: number };
    parsed.state = { ...(parsed.state ?? {}), accessToken: null, isAuthenticated: false, user: null };
    localStorage.setItem('gt-auth', JSON.stringify(parsed));
  } catch {
    // ignore
  }
}

export const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // send httpOnly refresh token cookie automatically
  headers: { 'Content-Type': 'application/json' },
});

// Attach access token from persisted store on each request
api.interceptors.request.use((config) => {
  const token = getPersistedToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// On 401: attempt silent token refresh then retry once
api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      try {
        const { data } = await axios.post(
          `${API_BASE_URL}/api/auth/refresh-token`,
          {},
          { withCredentials: true }
        );
        // Persist the new token so the store picks it up on next render
        setPersistedToken(data.accessToken);
        original.headers.Authorization = `Bearer ${data.accessToken}`;
        return api(original);
      } catch {
        clearPersistedToken();
        window.location.href = '/auth/login';
      }
    }
    return Promise.reject(error);
  }
);
