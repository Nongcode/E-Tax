import type { UseQueryResult } from '@tanstack/react-query';
import { Button, Empty, Result, Skeleton } from 'antd';
import type { ReactNode } from 'react';

interface QueryStateProps<T> {
  query: UseQueryResult<T>;
  isEmpty?: (d: T) => boolean;
  emptyText?: string;
  emptyAction?: ReactNode;
  children: (d: T) => ReactNode;
}

export function QueryState<T>({ query, isEmpty, emptyText, emptyAction, children }: QueryStateProps<T>) {
  if (query.isPending) return <Skeleton active />;
  if (query.isError) {
    return (
      <Result
        status="error"
        title="Không tải được dữ liệu"
        subTitle={query.error.message}
        extra={
          <Button type="primary" onClick={() => void query.refetch()}>
            Thử lại
          </Button>
        }
      />
    );
  }
  const data = query.data as T;
  if (isEmpty?.(data)) {
    return (
      <Empty description={emptyText}>
        {emptyAction}
      </Empty>
    );
  }
  return <>{children(data)}</>;
}
