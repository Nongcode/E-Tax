import { afterAll, beforeAll, beforeEach } from 'vitest';
import { setupServer } from 'msw/node';
import { createMockDb, type MockDb } from '../mocks/mockDb';
import { createMemoryStorage } from '../mocks/memoryStorage';
import { createHandlers } from '../mocks/handlers';

/** Gọi ở cấp cao nhất của file test. */
export function setupMockServer(): { db: () => MockDb } {
  const server = setupServer();
  let current: MockDb;

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  beforeEach(() => {
    current = createMockDb(createMemoryStorage());
    server.resetHandlers(...createHandlers(current));
  });
  afterAll(() => server.close());

  return { db: () => current };
}
