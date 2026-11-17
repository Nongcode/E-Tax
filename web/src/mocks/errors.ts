export class MockError extends Error {
  status: number;
  code: string;
  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = 'MockError';
    this.status = status;
    this.code = code;
  }
}

/** R23: dữ liệu gửi lên sai, message nói rõ trường sai. */
export const validationError = (message: string) => new MockError(400, 'VALIDATION_ERROR', message);
