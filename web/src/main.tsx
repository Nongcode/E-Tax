import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import dayjs from 'dayjs';
import 'dayjs/locale/vi';
import App from './App';

dayjs.locale('vi');

async function bootstrap() {
  if (import.meta.env.VITE_USE_MOCK === 'true') {
    // Service worker cần HTTPS hoặc localhost; mở qua IP mạng LAN sẽ lỗi — vẫn render để không trắng trang.
    try {
      const { worker } = await import('./mocks/browser');
      await worker.start({ onUnhandledRequest: 'bypass' });
    } catch (err) {
      console.warn('Không khởi động được API giả lập', err);
    }
  }
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}

void bootstrap();
