import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import type { ReactNode } from 'react';
import { AppShell } from './AppShell';
import { MeProvider } from './MeContext';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { RequestsPage } from '../features/requests/RequestsPage';
import { CreateRequestPage } from '../features/requests/CreateRequestPage';
import { RequestDetailPage } from '../features/requests/RequestDetailPage';
import { ReportsPage } from '../features/reports/ReportsPage';
import { EmptyState } from '../components/Ui';
import { useMe } from './MeContext';

export function App() {
  return <BrowserRouter><MeProvider><AppShell><Routes>
    <Route path="/" element={<DashboardPage />} />
    <Route path="/requests" element={<RequestsPage />} />
    <Route path="/requests/new" element={<HostOnly><CreateRequestPage /></HostOnly>} />
    <Route path="/requests/:requestId" element={<RequestDetailPage />} />
    <Route path="/reports" element={<ReportsPage />} />
    <Route path="/404" element={<EmptyState title="Page not found" message="The page you requested does not exist." />} />
    <Route path="*" element={<Navigate to="/404" replace />} />
  </Routes></AppShell></MeProvider></BrowserRouter>;
}

function HostOnly({ children }: { children: ReactNode }) {
  return useMe().role === 'HostRequester' ? children : <Navigate to="/" replace />;
}
