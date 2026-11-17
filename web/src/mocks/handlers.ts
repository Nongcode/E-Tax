import { delay, http, HttpResponse, type HttpHandler } from 'msw';
import type { MockDb } from './mockDb';
import { MockError } from './mockDb';
import { validationError } from './errors';
import type {
  AppRelease,
  BankSettings,
  ChangePasswordInput,
  LicenseStatus,
  LoginInput,
  OrderStatus,
  PlanInput,
  ProfileInput,
  RegisterInput,
  Role,
  User,
  UserStatus,
} from '../api/types';

const FORBIDDEN_MESSAGE = 'Bạn không có quyền thực hiện thao tác này';

type Access = 'public' | 'user' | Role;

interface Ctx {
  user: User;
  params: Record<string, string>;
  url: URL;
  body: <T>() => Promise<T>;
}

export function createHandlers(db: MockDb, opts: { delayMs?: number } = {}): HttpHandler[] {
  const delayMs = opts.delayMs ?? 0;

  function route(
    method: 'get' | 'post' | 'put' | 'patch' | 'delete',
    path: string,
    access: Access,
    fn: (c: Ctx) => unknown,
  ): HttpHandler {
    return http[method](`*/api${path}`, async ({ request, params }) => {
      if (delayMs > 0) await delay(delayMs);
      try {
        let user = null as unknown as User;
        if (access !== 'public') {
          const header = request.headers.get('Authorization');
          const token = header?.startsWith('Bearer ') ? header.slice(7) : null;
          user = db.userFromToken(token);
          if ((access === 'customer' || access === 'admin') && user.role !== access) {
            throw new MockError(403, 'FORBIDDEN', FORBIDDEN_MESSAGE);
          }
        }
        const result = await fn({
          user,
          params: params as Record<string, string>,
          url: new URL(request.url),
          body: async <T>() => {
            try {
              return (await request.json()) as T;
            } catch {
              throw validationError('Dữ liệu gửi lên không hợp lệ');
            }
          },
        });
        return result === undefined ? new HttpResponse(null, { status: 204 }) : HttpResponse.json(result as object);
      } catch (e) {
        if (e instanceof MockError) {
          return HttpResponse.json({ code: e.code, message: e.message }, { status: e.status });
        }
        throw e;
      }
    });
  }

  return [
    route('post', '/auth/register', 'public', async (c) => db.register(await c.body<RegisterInput>())),
    route('post', '/auth/login', 'public', async (c) => db.login(await c.body<LoginInput>())),
    route('get', '/auth/me', 'user', (c) => c.user),
    route('post', '/auth/forgot-password', 'public', () => undefined),
    route('put', '/auth/profile', 'user', async (c) => db.updateProfile(c.user.id, await c.body<ProfileInput>())),
    route('post', '/auth/change-password', 'user', async (c) =>
      db.changePassword(c.user.id, await c.body<ChangePasswordInput>()),
    ),
    route('get', '/plans', 'public', () => db.listPlans()),
    route('get', '/app/latest', 'public', () => db.getApp()),

    route('post', '/orders', 'customer', async (c) =>
      db.createOrder(c.user.id, (await c.body<{ planId: string }>()).planId),
    ),
    route('get', '/orders', 'customer', (c) => db.listOrders(c.user.id)),
    route('get', '/orders/:id', 'customer', (c) => db.getOrder(c.user.id, c.params.id)),
    route('post', '/orders/:id/cancel', 'customer', (c) => db.cancelOrder(c.user.id, c.params.id)),

    route('get', '/licenses', 'customer', (c) => db.listLicenses(c.user.id)),
    route('get', '/licenses/:id', 'customer', (c) => db.getLicense(c.user.id, c.params.id)),
    route('post', '/licenses/:id/activations', 'customer', async (c) =>
      db.addActivation(c.user.id, c.params.id, await c.body<{ machineId: string; machineName: string }>()),
    ),
    route('delete', '/licenses/:id/activations/:actId', 'customer', (c) =>
      db.removeActivation(c.user.id, c.params.id, c.params.actId),
    ),

    route('get', '/admin/stats', 'admin', () => db.stats()),
    route('get', '/admin/users', 'admin', (c) => db.adminListUsers(c.url.searchParams.get('q') || undefined)),
    route('get', '/admin/users/:id', 'admin', (c) => db.adminGetUser(c.params.id)),
    route('patch', '/admin/users/:id', 'admin', async (c) =>
      db.adminSetUserStatus(c.user.id, c.params.id, (await c.body<{ status: UserStatus }>()).status),
    ),
    route('get', '/admin/plans', 'admin', () => db.listPlans(true)),
    route('post', '/admin/plans', 'admin', async (c) => db.createPlan(await c.body<PlanInput>())),
    route('put', '/admin/plans/:id', 'admin', async (c) => db.updatePlan(c.params.id, await c.body<PlanInput>())),
    route('get', '/admin/orders', 'admin', (c) =>
      db.adminListOrders({
        status: (c.url.searchParams.get('status') || undefined) as OrderStatus | undefined,
        q: c.url.searchParams.get('q') || undefined,
      }),
    ),
    route('post', '/admin/orders/:id/confirm', 'admin', async (c) =>
      db.adminConfirmOrder(c.user.id, c.params.id, (await c.body<{ note?: string }>()).note),
    ),
    route('post', '/admin/orders/:id/cancel', 'admin', async (c) =>
      db.adminCancelOrder(c.params.id, (await c.body<{ note?: string }>()).note),
    ),
    route('get', '/admin/licenses', 'admin', (c) =>
      db.adminListLicenses({
        status: (c.url.searchParams.get('status') || undefined) as LicenseStatus | undefined,
        q: c.url.searchParams.get('q') || undefined,
      }),
    ),
    route('get', '/admin/licenses/:id', 'admin', (c) => db.adminGetLicense(c.params.id)),
    route('post', '/admin/licenses/:id/revoke', 'admin', (c) => db.revokeLicense(c.params.id)),
    route('post', '/admin/licenses/:id/extend', 'admin', async (c) =>
      db.extendLicense(c.params.id, (await c.body<{ months: number }>()).months),
    ),
    route('get', '/admin/settings/bank', 'admin', () => db.getBank()),
    route('put', '/admin/settings/bank', 'admin', async (c) => db.setBank(await c.body<BankSettings>())),
    route('get', '/admin/settings/app', 'admin', () => db.getApp()),
    route('put', '/admin/settings/app', 'admin', async (c) => db.setApp(await c.body<AppRelease>())),
  ];
}
