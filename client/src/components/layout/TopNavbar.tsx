import { useEffect, useRef, useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { Avatar } from '../common/Avatar'
import { SearchInput } from '../common/SearchInput'
import { useAuth } from '../../auth/useAuth'

const helpMessage = 'Need help with the visitor workflow? Use this panel for guidance on the current page. For workflow issues, contact the support team.'

export function TopNavbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [helpOpen, setHelpOpen] = useState(false)
  const closeHelpRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!helpOpen) return
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setHelpOpen(false)
    }
    document.addEventListener('keydown', handleEscape)
    closeHelpRef.current?.focus()
    return () => document.removeEventListener('keydown', handleEscape)
  }, [helpOpen])

  if (!user) return null

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="relative flex h-20 items-center justify-between border-b border-[var(--silver)] bg-white px-6 md:px-8">
      <div className="flex items-center gap-3 md:hidden">
        <span className="text-xs font-bold tracking-[0.25em] text-[var(--royal-blue)]">VMS</span>
      </div>

      <div className="hidden md:block">
        <SearchInput />
      </div>

      <div className="flex items-center gap-4">
        <button
          type="button"
          aria-label="Open help panel"
          aria-expanded={helpOpen}
          aria-controls="help-panel"
          onClick={() => setHelpOpen((current) => !current)}
          className="cursor-pointer text-sm text-[var(--muted)] hover:text-[var(--royal-blue)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--royal-blue)]"
        >
          Help
        </button>

        {helpOpen && (
          <section
            id="help-panel"
            role="dialog"
            aria-label="Help panel"
            className="absolute right-6 top-16 z-20 w-[min(22rem,calc(100vw-3rem))] border border-[var(--silver)] bg-white p-5 text-sm text-[var(--ink)] shadow-[0_4px_14px_rgba(20,32,48,0.12)] md:right-8"
          >
            <div className="flex items-start justify-between gap-4">
              <h2 className="display text-lg font-bold text-[var(--royal-blue)]">Help</h2>
              <button
                ref={closeHelpRef}
                type="button"
                aria-label="Close help panel"
                onClick={() => setHelpOpen(false)}
                className="cursor-pointer text-lg leading-none text-[var(--muted)] hover:text-[var(--royal-blue)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--royal-blue)]"
              >
                &times;
              </button>
            </div>
            <p className="mt-4 leading-6">{helpMessage}</p>
          </section>
        )}

        <NavLink to="/notifications" aria-label="Notifications" className="relative text-sm text-[var(--muted)] hover:text-[var(--royal-blue)]">
          Alerts
          <span className="absolute -right-2 -top-2 h-1.5 w-1.5 rounded-full bg-[#b34a42]" />
        </NavLink>

        <details className="group relative border-l border-[var(--silver)] pl-4">
          <summary className="flex cursor-pointer list-none items-center gap-3">
            <Avatar initials={user.initials} size="sm" />
            <span className="hidden text-left lg:block">
              <span className="block text-sm font-medium text-[var(--ink)]">{user.name}</span>
              <span className="block text-[10px] uppercase tracking-wide text-[var(--muted)]">{user.role}</span>
            </span>
          </summary>

          <div className="absolute right-0 top-12 z-10 w-56 border border-[var(--silver)] bg-white p-4 shadow-[0_4px_14px_rgba(20,32,48,0.12)]">
            <p className="text-sm font-semibold text-[var(--ink)]">{user.name}</p>
            <p className="mt-1 break-all text-xs text-[var(--muted)]">{user.email}</p>
            <p className="mt-3 border-t border-[var(--silver)] pt-3 text-xs font-semibold text-[var(--royal-blue)]">{user.role}</p>
            <button
              type="button"
              onClick={handleLogout}
              className="mt-4 w-full cursor-pointer border border-[var(--silver)] px-3 py-2 text-left text-xs font-semibold text-[var(--ink)] hover:bg-[var(--surface)]"
            >
              Log out
            </button>
          </div>
        </details>
      </div>
    </header>
  )
}
