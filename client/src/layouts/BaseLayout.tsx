import { NavLink, Outlet } from 'react-router-dom'
import { RRLogo } from '../components/common/RRLogo'

export function BaseLayout() {
  return (
    <div className="min-h-screen bg-[var(--surface)]">
      <header className="border-b border-[var(--silver)] bg-[var(--white)]">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-5">
          <NavLink to="/" className="flex items-center gap-3 text-[var(--royal-blue)]">
            <RRLogo size="small" ariaLabel="RRVMS" />
            <span className="display text-xl font-bold tracking-wide">Visitor Management</span>
          </NavLink>
          <div className="text-right text-xs uppercase tracking-[0.18em] text-[var(--muted)]">
            <div>RRVMS</div>
            <div className="mt-1 tracking-normal">Prototype environment</div>
          </div>
        </div>
      </header>
      <nav className="border-b border-[var(--silver)] bg-[var(--white)]">
        <div className="mx-auto flex max-w-7xl gap-8 px-6">
          <NavLink to="/" className={({ isActive }) => `border-b-2 py-3 text-sm ${isActive ? 'border-[var(--royal-blue)] font-semibold text-[var(--royal-blue)]' : 'border-transparent text-[var(--muted)]'}`} end>Overview</NavLink>
        </div>
      </nav>
      <main className="mx-auto max-w-7xl px-6 py-10"><Outlet /></main>
      <footer className="border-t border-[var(--silver)] bg-[var(--white)] px-6 py-5 text-center text-xs text-[var(--muted)]">Rolls-Royce Visitor Management System</footer>
    </div>
  )
}
