import { render, screen } from '@testing-library/react';
import { Button, DatePicker } from 'antd';
import { AppProviders } from './App';
import { theme } from './theme';

describe('AppProviders', () => {
  it('AppProviders áp dụng theme màu chủ đạo #0075D6 và locale vi', () => {
    render(
      <AppProviders>
        <Button type="primary">Mua</Button>
        <DatePicker open />
      </AppProviders>,
    );
    expect(screen.getByRole('button', { name: 'Mua' })).toBeInTheDocument();
    expect(theme.token?.colorPrimary).toBe('#0075D6');
    expect(document.body.textContent).toContain('Hôm nay');
  });
});
