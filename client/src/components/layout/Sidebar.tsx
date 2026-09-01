import { NavLink } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'
import { navigationByRole } from '../../auth/roles'

function LinkGroup({ items }: { items: Array<{ label: string; path: string }> }) {
  return <div className="space-y-1">{items.map(({ label, path }) => <NavLink key={path} to={path} className={({ isActive }) => `flex items-center border-l-2 px-5 py-2.5 text-sm transition-colors ${isActive ? 'border-[var(--royal-blue)] bg-[#e9eef6] font-semibold text-[var(--royal-blue)]' : 'border-transparent text-[#d9e0eb] hover:bg-[var(--royal-blue)] hover:text-white'}`}>{label}</NavLink>)}</div>
}

export function Sidebar() {
  const { user } = useAuth()
  if (!user) return null
  const grouped = navigationByRole[user.role].reduce<Record<string, Array<{ label: string; path: string }>>>((result, item) => { (result[item.group] ??= []).push(item); return result }, {})
  return <aside className="hidden w-64 shrink-0 bg-[var(--royal-blue)] text-white md:block"><div className="flex h-20 items-center gap-3 border-b border-[#28436c] px-6"><div className="flex h-9 w-9 items-center justify-center rounded-full bg-white/10 text-sm font-bold">VM</div><div className="display text-lg font-bold">Visitor Management</div></div><div className="px-0 py-7">{(['Workspace', 'Governance', 'Account'] as const).map((group) => grouped[group] ? <div key={group}><p className="px-5 pb-3 pt-2 text-[10px] font-semibold uppercase tracking-[0.22em] text-[#8ea5c7]">{group}</p><LinkGroup items={grouped[group]} /></div> : null)}</div><div className="mt-auto border-t border-[#28436c] px-5 py-5 text-xs text-[#bfcde0]">Visitor Management System<br /><span className="text-[#8098bc]">Operations portal</span></div></aside>
}
