import { useState, type ReactNode } from 'react'
import { mockUsers, type MockUser } from './roles'
import { AuthContext } from './context'

const sessionKey = 'rrvms.mock.session'

function readSession(): MockUser | null { const id = localStorage.getItem(sessionKey); return mockUsers.find((user) => user.id === id) ?? null }

export function AuthProvider({ children }: { children: ReactNode }) { const [user, setUser] = useState<MockUser | null>(() => readSession()); const login = (nextUser: MockUser) => { localStorage.setItem(sessionKey, nextUser.id); setUser(nextUser) }; const logout = () => { localStorage.removeItem(sessionKey); setUser(null) }; return <AuthContext.Provider value={{ user, isAuthenticated: user !== null, login, logout }}>{children}</AuthContext.Provider> }

