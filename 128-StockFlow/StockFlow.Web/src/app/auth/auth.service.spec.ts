import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors, withNoXsrfProtection } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { AuthService, csrfInterceptor } from './auth.service';

describe('Memory CSRF contract', () => {
  let auth: AuthService; let http: HttpClient; let requests: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(withNoXsrfProtection(), withInterceptors([csrfInterceptor])), provideHttpClientTesting()] });
    auth = TestBed.inject(AuthService); http = TestBed.inject(HttpClient); requests = TestBed.inject(HttpTestingController);
  });
  afterEach(() => requests.verify());

  it('sends the server JSON token only to unsafe same-origin API requests', async () => {
    const refresh = auth.refreshCsrf();
    requests.expectOne('/api/auth/csrf').flush({ requestToken: 'memory-token' }); await refresh;
    const mutation = firstValueFrom(http.post('/api/products', {}));
    const local = requests.expectOne('/api/products');
    expect(local.request.headers.get('X-XSRF-TOKEN')).toBe('memory-token');
    local.flush({}); await mutation;
    const foreign = firstValueFrom(http.post('https://external.example/api/products', {}));
    const remote = requests.expectOne('https://external.example/api/products');
    expect(remote.request.headers.has('X-XSRF-TOKEN')).toBe(false);
    remote.flush({}); await foreign;
    const read = firstValueFrom(http.get('/api/products'));
    const get = requests.expectOne('/api/products'); expect(get.request.headers.has('X-XSRF-TOKEN')).toBe(false); get.flush([]); await read;
  });

  it('blocks mutations when there is no in-memory request token', async () => {
    await expect(firstValueFrom(http.post('/api/products', {}))).rejects.toThrow('CSRF');
    requests.expectNone('/api/products');
  });

  it('fetches a new identity-bound token after login and logout', async () => {
    const login = auth.login('admin@example.com', 'operator-supplied');
    requests.expectOne('/api/auth/csrf').flush({ requestToken: 'anonymous-token' });
    await Promise.resolve(); await Promise.resolve();
    const request = requests.expectOne('/api/auth/login'); expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('anonymous-token'); request.flush(null);
    await Promise.resolve(); await Promise.resolve();
    requests.expectOne('/api/auth/me').flush({ id: 'user1', email: 'admin@example.com', roles: ['Admin'] });
    await Promise.resolve(); await Promise.resolve();
    requests.expectOne('/api/auth/csrf').flush({ requestToken: 'authenticated-token' }); await login;
    expect(auth.requestToken).toBe('authenticated-token');
    const logout = auth.logout();
    const end = requests.expectOne('/api/auth/logout'); expect(end.request.headers.get('X-XSRF-TOKEN')).toBe('authenticated-token'); end.flush(null);
    await Promise.resolve(); await Promise.resolve();
    requests.expectOne('/api/auth/csrf').flush({ requestToken: 'new-anonymous-token' }); await logout;
    expect(auth.user()).toBeNull(); expect(auth.requestToken).toBe('new-anonymous-token');
  });
});
