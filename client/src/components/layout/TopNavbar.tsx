import { NavLink, useNavigate } from 'react-router-dom'
import { Avatar } from '../common/Avatar'
import { SearchInput } from '../common/SearchInput'
import { useAuth } from '../../auth/useAuth'
import { RRLogo } from '../common/RRLogo'

export function TopNavbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  if (!user) return null
  const handleLogout = () => { logout(); navigate('/login', { replace: true }) }
  return <header className="flex h-20 items-center justify-between border-b border-[var(--silver)] bg-white px-6 md:px-8"><div className="flex items-center gap-3 md:hidden"><RRLogo size="small" ariaLabel="RRVMS" /><span className="text-xs font-bold tracking-[0.25em] text-[var(--royal-blue)]">RRVMS</span></div><div className="hidden md:block"><SearchInput /></div><div className="flex items-center gap-4"><button type="button" aria-label="Help" className="text-sm text-[var(--muted)] hover:text-[var(--royal-blue)]">Help</button><NavLink to="/notifications" aria-label="Notifications" className="relative text-sm text-[var(--muted)] hover:text-[var(--royal-blue)]">Alerts<span className="absolute -right-2 -top-2 h-1.5 w-1.5 rounded-full bg-[#b34a42]" /></NavLink><details className="group relative border-l border-[var(--silver)] pl-4"><summary className="flex cursor-pointer list-none items-center gap-3"><Avatar initials={user.initials} size="sm" /><span className="hidden text-left lg:block"><span className="block text-sm font-medium text-[var(--ink)]">{user.name}</span><span className="block text-[10px] uppercase tracking-wide text-[var(--muted)]">{user.role}</span></span></summary><div className="absolute right-0 top-12 z-10 w-56 border border-[var(--silver)] bg-white p-4 shadow-[0_4px_14px_rgba(20,32,48,0.12)]"><p className="text-sm font-semibold text-[var(--ink)]">{user.name}</p><p className="mt-1 break-all text-xs text-[var(--muted)]">{user.email}</p><p className="mt-3 border-t border-[var(--silver)] pt-3 text-xs font-semibold text-[var(--royal-blue)]">{user.role}</p><button type="button" onClick={handleLogout} className="mt-4 w-full border border-[var(--silver)] px-3 py-2 text-left text-xs font-semibold text-[var(--ink)] hover:bg-[var(--surface)]">Log out</button></div></details></div></header>
}
