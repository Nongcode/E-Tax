import { Navigate, Outlet, useLocation } from 'react-router-dom';
import type { Role } from '../api/types';
import ForbiddenPage from '../pages/ForbiddenPage';
import { useAuth } from './context';

export function RequireAuth() {
  const { user } = useAuth();
  const { pathname, search } = useLocation();
  if (!user) return <Navigate to={'/dang-nhap?next=' + encodeURIComponent(pathname + search)} replace />;
  return <Outlet />;
}

export function RequireRole({ role }: { role: Role }) {
  const { user } = useAuth();
  const { pathname, search } = useLocation();
  if (!user) return <Navigate to={'/dang-nhap?next=' + encodeURIComponent(pathname + search)} replace />;
  if (user.role !== role) return <ForbiddenPage />;
  return <Outlet />;
}
