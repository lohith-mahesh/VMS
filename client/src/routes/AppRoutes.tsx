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
import { VisitorFormPage } from '../pages/VisitorFormPage'
import { NotificationsPage, PendingActionsPage, ReceptionPage } from '../pages/OperationalPages'

const allRoles = ['HOST_REQUESTER', 'EXPORT_CONTROL', 'RECEPTION'] as const

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/visitor-forms/:id" element={<VisitorFormPage />} />
      <Route element={<ProtectedRoute />}>
        <Route path="/access-restricted" element={<AppLayout />}>
          <Route index element={<AccessRestrictedPage />} />
        </Route>
        <Route element={<AppLayout />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route element={<ProtectedRoute allowedRoles={[...allRoles]} />}>
            <Route path="/visitor-requests" element={<VisitorRequestsPage />} />
            <Route path="/visitor-requests/new" element={<CreateVisitorRequestPage />} />
            <Route path="/visitor-requests/:id" element={<VisitorRequestDetailPage />} />
          </Route>
          <Route element={<ProtectedRoute allowedRoles={['HOST_REQUESTER', 'EXPORT_CONTROL']} />}>
            <Route path="/pending-actions" element={<PendingActionsPage />} />
          </Route>
          <Route element={<ProtectedRoute allowedRoles={[...allRoles]} />}>
            <Route path="/notifications" element={<NotificationsPage />} />
          </Route>
          <Route element={<ProtectedRoute allowedRoles={['RECEPTION']} />}>
            <Route path="/reception" element={<ReceptionPage />} />
            <Route path="/todays-visits" element={<ReceptionPage />} />
          </Route>
          <Route element={<ProtectedRoute allowedRoles={['EXPORT_CONTROL']} />}>
            <Route path="/export-control" element={<PlaceholderPage />} />
            <Route path="/reports" element={<PlaceholderPage />} />
          </Route>
          <Route path="/profile" element={<PlaceholderPage />} />
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}
