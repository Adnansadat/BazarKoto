import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap } from 'rxjs';

import { Api } from './api';

export interface CurrentUser {
  email: string;
  role: string;
  expiresAt: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  email: string;
  role: string;
}

export interface UpdateAdminEmailRequest {
  newEmail: string;
  currentPassword: string;
}

export interface UpdateAdminPasswordRequest {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface UpdateAdminCredentialsRequest {
  oldEmail: string;
  newEmail: string;
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly accessTokenKey = 'bazarKoto.accessToken';
  private readonly refreshTokenKey = 'bazarKoto.refreshToken';
  private readonly emailKey = 'bazarKoto.email';
  private readonly roleKey = 'bazarKoto.role';
  private readonly expiresAtKey = 'bazarKoto.expiresAt';
  private readonly adminState = new BehaviorSubject<boolean>(this.isCurrentAdmin());
  readonly isAdmin$ = this.adminState.asObservable();

  constructor(private readonly api: Api) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('/Auth/login', { email, password }).pipe(
      tap(response => {
        if (!this.isBrowser) {
          return;
        }

        localStorage.setItem(this.accessTokenKey, response.accessToken);
        localStorage.setItem(this.refreshTokenKey, response.refreshToken);
        localStorage.setItem(this.emailKey, response.email);
        localStorage.setItem(this.roleKey, response.role);
        localStorage.setItem(this.expiresAtKey, response.expiresAt);
        this.adminState.next(this.isCurrentAdmin());
      })
    );
  }

  logout(): void {
    if (!this.isBrowser) {
      this.adminState.next(false);
      return;
    }

    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.emailKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.expiresAtKey);
    this.adminState.next(false);
  }

  logoutFromServer(): Observable<void> {
    return this.api.postResponse<object>('/Auth/logout', {}).pipe(
      map(() => undefined),
      catchError(() => of(undefined)),
      tap(() => this.logout())
    );
  }

  updateAdminEmail(request: UpdateAdminEmailRequest): Observable<void> {
    return this.api.patch<object>('/Auth/admin/email', request).pipe(map(() => undefined));
  }

  updateAdminPassword(request: UpdateAdminPasswordRequest): Observable<void> {
    return this.api.patch<object>('/Auth/admin/password', request).pipe(map(() => undefined));
  }

  updateAdminCredentials(request: UpdateAdminCredentialsRequest): Observable<void> {
    return this.api.patch<object>('/Auth/admin/credentials', request).pipe(map(() => undefined));
  }

  isAuthenticated(): boolean {
    if (!this.isBrowser) {
      return false;
    }

    const token = this.getAccessToken();
    const expiresAt = localStorage.getItem(this.expiresAtKey);

    if (!token || !expiresAt) {
      return false;
    }

    return new Date(expiresAt).getTime() > Date.now();
  }

  getAccessToken(): string | null {
    if (!this.isBrowser) {
      return null;
    }

    return localStorage.getItem(this.accessTokenKey);
  }

  getRefreshToken(): string | null {
    if (!this.isBrowser) {
      return null;
    }

    return localStorage.getItem(this.refreshTokenKey);
  }

  getCurrentUser(): CurrentUser | null {
    if (!this.isBrowser) {
      return null;
    }

    const email = localStorage.getItem(this.emailKey);
    const role = localStorage.getItem(this.roleKey);
    const expiresAt = localStorage.getItem(this.expiresAtKey);

    if (!email || !role || !expiresAt) {
      return null;
    }

    return { email, role, expiresAt };
  }

  hasRole(role: string): boolean {
    if (!this.isBrowser) {
      return false;
    }

    return localStorage.getItem(this.roleKey) === role;
  }

  isCurrentAdmin(): boolean {
    return this.isAuthenticated() && this.hasRole('Admin');
  }
}
