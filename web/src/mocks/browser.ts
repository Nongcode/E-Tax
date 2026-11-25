import { setupWorker } from 'msw/browser';
import { createMockDb } from './mockDb';
import { createHandlers } from './handlers';

export const mockDb = createMockDb(localStorage);
export const worker = setupWorker(...createHandlers(mockDb, { delayMs: 300 }));
