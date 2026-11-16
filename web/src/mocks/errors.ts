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
