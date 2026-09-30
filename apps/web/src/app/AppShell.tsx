import { useState, type ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { developmentRole, isDevelopmentAuthentication, setDevelopmentRole, signOut } from '../auth/auth';
import type { UserRole } from '../lib/types';
import { useMe } from './MeContext';

const roleLabel: Record<UserRole, string> = {
  HostRequester: 'Host / Requester',
  ExportControl: 'Export Control',
  Reception: 'Reception'
};

export function AppShell({ children }: { children: ReactNode }) {
  const me = useMe();
  const [open, setOpen] = useState(false);
  const links = [
    { to: '/', label: 'Dashboard', icon: '▦', visible: true },
    { to: '/requests', label: me.role === 'Reception' ? 'Reception queue' : me.role === 'ExportControl' ? 'Screening queue' : 'My requests', icon: '▤', visible: true },
    { to: '/requests/new', label: 'New request', icon: '+', visible: me.role === 'HostRequester' },
    { to: '/reports', label: 'Reports', icon: '◫', visible: true }
  ];
  return <div className="app-shell">
    <a className="skip-link" href="#main-content">Skip to main content</a>
    <header className="topbar">
      <button className="menu-button" type="button" aria-label="Toggle navigation" aria-expanded={open} onClick={() => setOpen(value => !value)}>☰</button>
      <NavLink className="brand" to="/" aria-label="RRVMS home"><span className="brand__mark">RR</span><span><strong>RRVMS</strong><small>Visitor Management</small></span></NavLink>
      <div className="profile">
        <span className="avatar" aria-hidden="true">{initials(me.displayName)}</span>
        <span className="profile__text"><strong>{me.displayName}</strong><small>{roleLabel[me.role]}</small></span>
        <button className="button button--text" type="button" onClick={() => void signOut()}>Sign out</button>
      </div>
    </header>
    <aside className={`sidebar ${open ? 'sidebar--open' : ''}`}>
      <nav aria-label="Primary navigation">
        {links.filter(link => link.visible).map(link => <NavLink key={link.to} to={link.to} end={link.to === '/'} className={({ isActive }) => isActive ? 'active' : undefined} onClick={() => setOpen(false)}><span aria-hidden="true">{link.icon}</span>{link.label}</NavLink>)}
      </nav>
      {isDevelopmentAuthentication && <label className="dev-role"><span>Development role</span><select value={developmentRole()} onChange={event => setDevelopmentRole(event.target.value as UserRole)}><option value="HostRequester">Host / Requester</option><option value="ExportControl">Export Control</option><option value="Reception">Reception</option></select></label>}
    </aside>
    <main id="main-content" className="main-content">{children}</main>
    <footer className="footer"><span>RRVMS</span><span>Secure visitor operations</span></footer>
  </div>;
}

function initials(name: string): string {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map(value => value[0]?.toUpperCase()).join('');
}
