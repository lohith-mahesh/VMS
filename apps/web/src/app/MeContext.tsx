import { createContext, type ReactNode, useContext, useEffect, useState } from 'react';
import { api, ApiError } from '../lib/api';
import type { Me } from '../lib/types';
import { Alert, Loading } from '../components/Ui';

const MeContext = createContext<Me | undefined>(undefined);

export function MeProvider({ children }: { children: ReactNode }) {
  const [me, setMe] = useState<Me>();
  const [error, setError] = useState('');
  useEffect(() => {
    const controller = new AbortController();
    api<Me>('/api/me', { signal: controller.signal })
      .then(setMe)
      .catch(value => {
        if (value instanceof DOMException && value.name === 'AbortError') return;
        setError(value instanceof ApiError ? value.message : 'Unable to load your RRVMS profile.');
      });
    return () => controller.abort();
  }, []);
  if (error) return <main className="centered"><Alert tone="danger" title="Access unavailable">{error}</Alert></main>;
  if (!me) return <main className="centered"><Loading label="Signing you in" /></main>;
  return <MeContext.Provider value={me}>{children}</MeContext.Provider>;
}

export function useMe(): Me {
  const value = useContext(MeContext);
  if (!value) throw new Error('useMe must be used inside MeProvider.');
  return value;
}
