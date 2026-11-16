import { Outlet } from 'react-router-dom';
import { AuthProvider } from '../auth/AuthProvider';
import DevToolbar from '../dev/DevToolbar';
import { ErrorBoundary } from '../shared/ErrorBoundary';

export default function RootLayout() {
  return (
    <AuthProvider>
      <ErrorBoundary>
        <DevToolbar />
        <Outlet />
      </ErrorBoundary>
    </AuthProvider>
  );
}
