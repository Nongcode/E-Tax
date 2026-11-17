import type { ReactNode } from 'react';
import { App as AntApp, ConfigProvider } from 'antd';
import viVN from 'antd/locale/vi_VN';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { shouldRetry } from './api/client';
import { theme } from './theme';
import { router } from './router';

const defaultQueryClient = new QueryClient({
  defaultOptions: { queries: { retry: shouldRetry, refetchOnWindowFocus: false } },
});

export function AppProviders({
  children,
  queryClient = defaultQueryClient,
}: {
  children: ReactNode;
  queryClient?: QueryClient;
}) {
  return (
    <ConfigProvider theme={theme} locale={viVN}>
      <AntApp>
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      </AntApp>
    </ConfigProvider>
  );
}

export default function App() {
  return (
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>
  );
}
