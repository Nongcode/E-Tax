import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { AppProviders } from '../App';
import { ApiError } from '../api/client';
import DevToolbar from './DevToolbar';

const login = vi.fn();
vi.mock('../auth/context', () => ({ useAuth: () => ({ login }) }));

afterEach(() => {
  vi.unstubAllEnvs();
  login.mockReset();
});

describe('DevToolbar', () => {
  it('đăng nhập nhanh lỗi thì hiện thông báo', async () => {
    vi.stubEnv('VITE_USE_MOCK', 'true');
    login.mockRejectedValue(new ApiError(423, 'ACCOUNT_LOCKED', 'Tài khoản đã bị khoá'));
    const { container } = render(
      <AppProviders>
        <MemoryRouter>
          <DevToolbar />
        </MemoryRouter>
      </AppProviders>,
    );
    await userEvent.click(container.querySelector('.ant-float-btn')!);
    await userEvent.click(container.querySelector('.anticon-user')!);
    expect(await screen.findByText('Tài khoản đã bị khoá')).toBeInTheDocument();
  });
});
