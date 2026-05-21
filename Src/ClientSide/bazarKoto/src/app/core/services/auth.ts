import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

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

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly accessTokenKey = 'bazarKoto.accessToken';
  private readonly refreshTokenKey = 'bazarKoto.refreshToken';
  private readonly emailKey = 'bazarKoto.email';
  private readonly roleKey = 'bazarKoto.role';
  private readonly expiresAtKey = 'bazarKoto.expiresAt';

  constructor(private readonly api: Api) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('/Auth/login', { email, password }).pipe(
      tap(response => {
        localStorage.setItem(this.accessTokenKey, response.accessToken);
        localStorage.setItem(this.refreshTokenKey, response.refreshToken);
        localStorage.setItem(this.emailKey, response.email);
        localStorage.setItem(this.roleKey, response.role);
        localStorage.setItem(this.expiresAtKey, response.expiresAt);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.emailKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.expiresAtKey);
  }

  isAuthenticated(): boolean {
    const token = this.getAccessToken();
    const expiresAt = localStorage.getItem(this.expiresAtKey);

    if (!token || !expiresAt) {
      return false;
    }

    return new Date(expiresAt).getTime() > Date.now();
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.accessTokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  getCurrentUser(): CurrentUser | null {
    const email = localStorage.getItem(this.emailKey);
    const role = localStorage.getItem(this.roleKey);
    const expiresAt = localStorage.getItem(this.expiresAtKey);

    if (!email || !role || !expiresAt) {
      return null;
    }

    return { email, role, expiresAt };
  }

  hasRole(role: string): boolean {
    return localStorage.getItem(this.roleKey) === role;
  }
}
