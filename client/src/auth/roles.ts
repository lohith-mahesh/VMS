export const roles = ['Requester', 'Host', 'ExportControl', 'Security', 'Admin'] as const
export type Role = (typeof roles)[number]

export type MockUser = { id: string; name: string; email: string; role: Role; initials: string }

export const mockUsers: MockUser[] = [
  { id: 'prototype-requester', name: 'Alex Morgan', email: 'alex.morgan@rolls-royce.com', role: 'Requester', initials: 'AM' },
  { id: 'prototype-host', name: 'Daniel Carter', email: 'daniel.carter@rolls-royce.com', role: 'Host', initials: 'DC' },
  { id: 'prototype-export-control', name: 'Priya Shah', email: 'priya.shah@rolls-royce.com', role: 'ExportControl', initials: 'PS' },
  { id: 'prototype-security', name: 'Michael Brown', email: 'michael.brown@rolls-royce.com', role: 'Security', initials: 'MB' },
  { id: 'prototype-admin', name: 'Admin User', email: 'admin@rolls-royce.com', role: 'Admin', initials: 'AU' },
]

export const navigationByRole: Record<Role, Array<{ label: string; path: string; group: 'Workspace' | 'Governance' | 'Account' }>> = {
  Requester: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' }, { label: 'Visitor requests', path: '/visitor-requests', group: 'Workspace' }, { label: 'My visitors', path: '/my-visitors', group: 'Workspace' }, { label: "Today's visits", path: '/todays-visits', group: 'Workspace' }, { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' }, { label: 'Notifications', path: '/notifications', group: 'Account' }, { label: 'Profile', path: '/profile', group: 'Account' },
  ],
  Host: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' }, { label: 'Visitor requests', path: '/visitor-requests', group: 'Workspace' }, { label: 'My visitors', path: '/my-visitors', group: 'Workspace' }, { label: 'Today\'s visits', path: '/todays-visits', group: 'Workspace' }, { label: 'Pending actions', path: '/pending-actions', group: 'Workspace' }, { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' }, { label: 'Notifications', path: '/notifications', group: 'Account' }, { label: 'Profile', path: '/profile', group: 'Account' },
  ],
  ExportControl: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' }, { label: 'Pending actions', path: '/pending-actions', group: 'Workspace' }, { label: 'Export control', path: '/export-control', group: 'Governance' }, { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' }, { label: 'Reports', path: '/reports', group: 'Governance' }, { label: 'Notifications', path: '/notifications', group: 'Account' }, { label: 'Profile', path: '/profile', group: 'Account' },
  ],
  Security: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' }, { label: "Today's visits", path: '/todays-visits', group: 'Workspace' }, { label: 'Security', path: '/security', group: 'Governance' }, { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' }, { label: 'Notifications', path: '/notifications', group: 'Account' }, { label: 'Profile', path: '/profile', group: 'Account' },
  ],
  Admin: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' }, { label: 'Visitor requests', path: '/visitor-requests', group: 'Workspace' }, { label: 'Pending actions', path: '/pending-actions', group: 'Workspace' }, { label: "Today's visits", path: '/todays-visits', group: 'Workspace' }, { label: 'Export control', path: '/export-control', group: 'Governance' }, { label: 'Security', path: '/security', group: 'Governance' }, { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' }, { label: 'Reports', path: '/reports', group: 'Governance' }, { label: 'Notifications', path: '/notifications', group: 'Account' }, { label: 'Settings', path: '/settings', group: 'Account' }, { label: 'Profile', path: '/profile', group: 'Account' },
  ],
}
