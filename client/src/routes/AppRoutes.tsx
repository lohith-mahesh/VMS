import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from '../auth/ProtectedRoute'
import { AppLayout } from '../layouts/AppLayout'
import { DashboardPage } from '../pages/DashboardPage'
import { PlaceholderPage } from '../pages/PlaceholderPage'
import { LoginPage } from '../pages/LoginPage'
import { AccessRestrictedPage } from '../pages/AccessRestrictedPage'
import { VisitorRequestsPage } from '../pages/VisitorRequestsPage'
import { CreateVisitorRequestPage } from '../pages/CreateVisitorRequestPage'
import { VisitorRequestDetailPage } from '../pages/VisitorRequestDetailPage'
import { NotificationsPage, PendingActionsPage, SecurityPage } from '../pages/OperationalPages'

const rolePaths = [
  { path: '/my-visitors', roles: ['Requester', 'Host', 'Admin'] as const },
  { path: '/todays-visits', roles: ['Requester', 'Host', 'Security', 'Admin'] as const },
  { path: '/visitor-history', roles: ['Requester', 'Host', 'ExportControl', 'Security', 'Admin'] as const },
  { path: '/settings', roles: ['Admin'] as const },
  { path: '/profile', roles: ['Requester', 'Host', 'ExportControl', 'Security', 'Admin'] as const },
]

export function AppRoutes() { return <Routes><Route path="/login" element={<LoginPage />} /><Route element={<ProtectedRoute />}><Route path="/access-restricted" element={<AppLayout />}><Route index element={<AccessRestrictedPage />} /></Route><Route element={<AppLayout />}><Route path="/dashboard" element={<DashboardPage />} /><Route element={<ProtectedRoute allowedRoles={['Requester', 'Host', 'Admin']} />}><Route path="/visitor-requests" element={<VisitorRequestsPage />} /><Route path="/visitor-requests/new" element={<CreateVisitorRequestPage />} /><Route path="/visitor-requests/:id" element={<VisitorRequestDetailPage />} /></Route><Route element={<ProtectedRoute allowedRoles={['Host', 'ExportControl', 'Admin']} />}><Route path="/pending-actions" element={<PendingActionsPage />} /></Route><Route element={<ProtectedRoute allowedRoles={['Requester', 'Host', 'ExportControl', 'Security', 'Admin']} />}><Route path="/notifications" element={<NotificationsPage />} /></Route><Route element={<ProtectedRoute allowedRoles={['Admin', 'Security']} />}><Route path="/security" element={<SecurityPage />} /></Route>{rolePaths.map(({ path, roles }) => <Route key={path} element={<ProtectedRoute allowedRoles={[...roles]} />}><Route path={path} element={<PlaceholderPage />} /></Route>)}<Route element={<ProtectedRoute allowedRoles={['Admin', 'ExportControl']} />}><Route path="/export-control" element={<PlaceholderPage />} /><Route path="/reports" element={<PlaceholderPage />} /></Route><Route path="/" element={<Navigate to="/dashboard" replace />} /><Route path="*" element={<Navigate to="/dashboard" replace />} /></Route></Route></Routes> }
