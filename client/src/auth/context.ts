import { createContext } from 'react'
import type { MockUser } from './roles'

export type AuthContextValue = { user: MockUser | null; isAuthenticated: boolean; login: (user: MockUser) => void; logout: () => void }
export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
