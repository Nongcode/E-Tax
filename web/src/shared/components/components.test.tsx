import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { UseQueryResult } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { AppProviders } from '../../App';
import ForbiddenPage from '../../pages/ForbiddenPage';
import NotFoundPage from '../../pages/NotFoundPage';
import { ErrorBoundary } from '../ErrorBoundary';
import { CopyField } from './CopyField';
import { QueryState } from './QueryState';
import { StatusTag } from './StatusTag';

function wrap(ui: React.ReactNode) {
  return render(
    <AppProviders>
      <MemoryRouter>{ui}</MemoryRouter>
    </AppProviders>,
  );
}

function fakeQuery<T>(partial: Partial<UseQueryResult<T>>) {
  return partial as UseQueryResult<T>;
}

describe('StatusTag', () => {
  it('hiện nhãn đơn hàng và bản quyền', () => {
    wrap(
      <>
        <StatusTag kind="order" status="pending" />
        <StatusTag kind="license" status="revoked" />
      </>,
    );
    expect(screen.getByText('Chờ thanh toán')).toBeInTheDocument();
    expect(screen.getByText('Đã thu hồi')).toBeInTheDocument();
  });
});

describe('CopyField', () => {
  it('sao chép giá trị vào clipboard và báo thành công', async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    wrap(<CopyField value="ABC-123" />);
    await user.click(screen.getByRole('button', { name: /Sao chép/ }));
    expect(writeText).toHaveBeenCalledWith('ABC-123');
    expect(await screen.findByText('Đã sao chép')).toBeInTheDocument();
  });

  it('báo lỗi khi clipboard thất bại', async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockRejectedValue(new Error('denied'));
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    wrap(<CopyField value="x" />);
    await user.click(screen.getByRole('button', { name: /Sao chép/ }));
    expect(
      await screen.findByText('Không sao chép được, hãy chọn và sao chép thủ công'),
    ).toBeInTheDocument();
  });
});

describe('QueryState', () => {
  it('lỗi thì hiện thông báo và nút Thử lại gọi refetch', async () => {
    const user = userEvent.setup();
    const refetch = vi.fn();
    wrap(
      <QueryState query={fakeQuery<string[]>({ isLoading: false, isError: true, error: new Error('Hỏng'), refetch } as never)}>
        {() => <div>nội dung</div>}
      </QueryState>,
    );
    expect(screen.getByText('Không tải được dữ liệu')).toBeInTheDocument();
    expect(screen.getByText('Hỏng')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Thử lại/ }));
    expect(refetch).toHaveBeenCalled();
  });

  it('dữ liệu rỗng thì hiện emptyText', () => {
    wrap(
      <QueryState
        query={fakeQuery<string[]>({ isLoading: false, isError: false, data: [] })}
        isEmpty={(d) => d.length === 0}
        emptyText="Chưa có dữ liệu"
      >
        {() => <div>nội dung</div>}
      </QueryState>,
    );
    expect(screen.getByText('Chưa có dữ liệu')).toBeInTheDocument();
  });

  it('có dữ liệu thì render children', () => {
    wrap(
      <QueryState query={fakeQuery<string[]>({ isLoading: false, isError: false, data: ['a'] })}>
        {(d) => <div>có {d.length} mục</div>}
      </QueryState>,
    );
    expect(screen.getByText('có 1 mục')).toBeInTheDocument();
  });
});

describe('ErrorBoundary', () => {
  it('hiện thông báo khi con ném lỗi', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    function Boom(): never {
      throw new Error('boom');
    }
    wrap(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>,
    );
    expect(screen.getByText('Đã có lỗi xảy ra')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Tải lại trang/ })).toBeInTheDocument();
    spy.mockRestore();
  });
});

describe('trang lỗi', () => {
  it('404', () => {
    wrap(<NotFoundPage />);
    expect(screen.getByText('Không tìm thấy trang')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Về trang chủ/ })).toBeInTheDocument();
  });
  it('403', () => {
    wrap(<ForbiddenPage />);
    expect(screen.getByText('Không có quyền truy cập')).toBeInTheDocument();
    expect(screen.getByText('Trang này chỉ dành cho quản trị viên.')).toBeInTheDocument();
  });
});
