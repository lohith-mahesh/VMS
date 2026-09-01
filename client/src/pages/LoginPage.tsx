import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { mockUsers, type MockUser } from '../auth/roles'
import { Button } from '../components/common/Button'

export function LoginPage() {
  const [showUsers, setShowUsers] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const destination = (location.state as { from?: string } | null)?.from ?? '/dashboard'

  const signIn = (user: MockUser) => {
    login(user)
    navigate(destination, { replace: true })
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-[var(--surface)] px-6 py-12">
      <div className="w-full max-w-md border border-[var(--silver)] bg-white shadow-[0_4px_18px_rgba(20,32,48,0.08)]">
        <div className="border-b border-[var(--silver)] bg-[var(--royal-blue)] px-8 py-7 text-white">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-white/15 text-2xl font-bold">VM</div>
          <h1 className="display mt-4 text-3xl font-bold">Visitor Management</h1>
          <p className="mt-2 text-sm text-[#d9e0eb]">A secure welcome starts here.</p>
        </div>
        <div className="p-8">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">Sign in</p>
          <h2 className="display mt-2 text-2xl font-bold text-[var(--royal-blue)]">Welcome</h2>
          <p className="mt-3 text-sm leading-6 text-[var(--muted)]">Use your secure access account to enter the visitor workflow.</p>
          {!showUsers ? <Button type="button" onClick={() => setShowUsers(true)} className="mt-7 w-full">Continue to sign in <span className="ml-2">-&gt;</span></Button> : <div className="mt-7">
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[var(--muted)]">Continue with prototype user</p>
            <div className="mt-3 space-y-2">
              {mockUsers.map((user) => <button key={user.id} type="button" onClick={() => signIn(user)} className="flex w-full items-center justify-between border border-[var(--silver)] px-4 py-3 text-left hover:bg-[var(--surface)]"><span><span className="block text-sm font-semibold text-[var(--ink)]">{user.name}</span><span className="block text-xs text-[var(--muted)]">{user.role}</span></span><span className="text-xs text-[var(--royal-blue)]">Select -&gt;</span></button>)}
            </div>
          </div>}
          <p className="mt-8 border-t border-[var(--silver)] pt-4 text-center text-[10px] uppercase tracking-[0.12em] text-[var(--muted)]">Development authentication environment</p>
        </div>
      </div>
    </div>
  )
}
