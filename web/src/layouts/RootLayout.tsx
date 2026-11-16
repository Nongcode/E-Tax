import { Outlet, useLocation } from 'react-router-dom';
import { AuthProvider } from '../auth/AuthProvider';
import DevToolbar from '../dev/DevToolbar';
import { ErrorBoundary } from '../shared/ErrorBoundary';

export default function RootLayout() {
  const { pathname } = useLocation();
  return (
    <AuthProvider>
      <ErrorBoundary key={pathname}>
        <DevToolbar />
        <Outlet />
      </ErrorBoundary>
    </AuthProvider>
  );
}
