import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const api = `${environment.apiBaseUrl}/api`;
  // Only send credentials to this application's protected API endpoints.
  const protectedRequest = req.url.startsWith(`${api}/admin/`) ||
    req.url.startsWith(`${api}/portfolio/manage`) || req.url === `${api}/portfolio/sync` ||
    req.url === `${api}/quo-messaging/test`;
  if (!protectedRequest) return next(req);

  const token = auth.isAuthenticated() ? auth.getToken() : null;
  return next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req).pipe(
    catchError(error => {
      if (error.status === 401) {
        auth.logout();
        void router.navigateByUrl('/admin/login');
      }
      return throwError(() => error);
    })
  );
};
