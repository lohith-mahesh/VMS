import { useState, type ReactNode } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { developmentRole, isDevelopmentAuthentication, setDevelopmentRole, signOut } from '../auth/auth';
import type { UserRole } from '../lib/types';
import { useMe } from './MeContext';

const roleLabel: Record<UserRole, string> = {
  HostRequester: 'Host / Requester',
  ExportControl: 'Export Control',
  Reception: 'Security / Reception'
};

export function AppShell({ children }: { children: ReactNode }) {
  const me = useMe();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const links = me.role === 'HostRequester'
    ? [{ to: '/', label: 'Dashboard' }, { to: '/requests', label: 'Visitor Requests' }, { to: '/history', label: 'Visitor History' }]
    : me.role === 'ExportControl'
      ? [{ to: '/', label: 'Dashboard' }, { to: '/pending', label: 'Pending Actions' }, { to: '/history', label: 'Visitor History' }, { to: '/reports', label: 'Reports' }]
      : [{ to: '/', label: 'Dashboard' }, { to: '/history', label: 'Visitor History' }, { to: '/reports', label: 'Reports' }];
  return <div className="app-shell">
    <a className="skip-link" href="#main-content">Skip to main content</a>
    <header className="topbar">
      <div className="header-main">
        <NavLink className="brand" to="/" aria-label="RR Visitor Management System home"><img className="brand-logo" src="/brand.svg" alt="" /><span><strong>RR Visitor Management System</strong><small>Visitor Management</small></span></NavLink>
        <nav className={`horizontal-nav ${open ? 'horizontal-nav--open' : ''}`} aria-label="Primary navigation">
          {links.map(link => <NavLink key={link.to} to={link.to} end={link.to === '/'} className={({ isActive }) => isActive ? 'active' : undefined} onClick={() => setOpen(false)}>{link.label}</NavLink>)}
        </nav>
        <form className="global-search" role="search" onSubmit={event => { event.preventDefault(); const value = new FormData(event.currentTarget).get('q')?.toString().trim(); if (value) navigate(`/requests?search=${encodeURIComponent(value)}`); }}><label className="visually-hidden" htmlFor="global-search">Search visitors</label><input id="global-search" name="q" placeholder="Search visitors" /></form>
        <div className="profile">
          <button className="menu-button" type="button" aria-label="Toggle navigation" aria-expanded={open} onClick={() => setOpen(value => !value)}>☰</button>
          <span className="avatar" aria-hidden="true">{initials(me.displayName)}</span>
          <span className="profile__text"><strong>{me.displayName}</strong><small>{roleLabel[me.role]}</small></span>
          <details className="profile-menu"><summary aria-label="Account menu">▾</summary><div><strong>{me.displayName}</strong><small>{roleLabel[me.role]}</small><button className="button button--text" type="button" onClick={() => void signOut()}>Sign out</button></div></details>
        </div>
      </div>
      {isDevelopmentAuthentication && <label className="dev-role"><span>Development role</span><select value={developmentRole()} onChange={event => setDevelopmentRole(event.target.value as UserRole)}><option value="HostRequester">Host / Requester</option><option value="ExportControl">Export Control</option><option value="Reception">Reception</option></select></label>}
    </header>
    <main id="main-content" className="main-content">{children}</main>
    <footer className="footer"><span>RR Visitor Management System</span><span>Visitor access and reception operations</span></footer>
  </div>;
}

function initials(name: string): string {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map(value => value[0]?.toUpperCase()).join('');
}
