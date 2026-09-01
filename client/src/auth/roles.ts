export const roles = ['HOST_REQUESTER', 'EXPORT_CONTROL', 'RECEPTION'] as const
export type Role = (typeof roles)[number]

export type MockUser = { id: string; name: string; email: string; role: Role; initials: string }

export const mockUsers: MockUser[] = [
  { id: 'prototype-host-requester', name: 'Alex Morgan', email: 'alex.morgan@demo.local', role: 'HOST_REQUESTER', initials: 'AM' },
  { id: 'prototype-export-control', name: 'Priya Shah', email: 'priya.shah@demo.local', role: 'EXPORT_CONTROL', initials: 'PS' },
  { id: 'prototype-reception', name: 'Michael Brown', email: 'michael.brown@demo.local', role: 'RECEPTION', initials: 'MB' },
]

export const navigationByRole: Record<Role, Array<{ label: string; path: string; group: 'Workspace' | 'Governance' | 'Account' }>> = {
  HOST_REQUESTER: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' },
    { label: 'Visitor requests', path: '/visitor-requests', group: 'Workspace' },
    { label: 'My visitors', path: '/my-visitors', group: 'Workspace' },
    { label: "Today's visits", path: '/todays-visits', group: 'Workspace' },
    { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' },
    { label: 'Notifications', path: '/notifications', group: 'Account' },
    { label: 'Profile', path: '/profile', group: 'Account' },
  ],
  EXPORT_CONTROL: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' },
    { label: 'Pending actions', path: '/pending-actions', group: 'Workspace' },
    { label: 'Export control', path: '/export-control', group: 'Governance' },
    { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' },
    { label: 'Reports', path: '/reports', group: 'Governance' },
    { label: 'Notifications', path: '/notifications', group: 'Account' },
    { label: 'Profile', path: '/profile', group: 'Account' },
  ],
  RECEPTION: [
    { label: 'Dashboard', path: '/dashboard', group: 'Workspace' },
    { label: "Today's visits", path: '/todays-visits', group: 'Workspace' },
    { label: 'Reception', path: '/reception', group: 'Governance' },
    { label: 'Visitor history', path: '/visitor-history', group: 'Workspace' },
    { label: 'Notifications', path: '/notifications', group: 'Account' },
    { label: 'Profile', path: '/profile', group: 'Account' },
  ],
}
