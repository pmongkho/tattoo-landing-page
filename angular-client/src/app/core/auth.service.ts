import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'admin_access_token';
  private readonly api = `${environment.apiBaseUrl}/api/auth`;

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<void> {
    return this.http.post<{accessToken: string}>(`${this.api}/login`, { email, password })
      .pipe(tap((res) => localStorage.setItem(this.tokenKey, res.accessToken)), map(() => void 0));
  }

  getToken(): string | null { return localStorage.getItem(this.tokenKey); }
  logout(): void { localStorage.removeItem(this.tokenKey); }
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
      if (typeof payload.exp === 'number' && payload.exp * 1000 > Date.now()) return true;
    } catch { /* Clear malformed or obsolete saved tokens. */ }
    this.logout();
    return false;
  }
}
