import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './useAuth'
import type { Role } from './roles'

export function ProtectedRoute({ allowedRoles }: { allowedRoles?: Role[] }) { const { user } = useAuth(); const location = useLocation(); if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />; if (allowedRoles && !allowedRoles.includes(user.role)) return <Navigate to="/access-restricted" replace state={{ from: location.pathname }} />; return <Outlet /> }
