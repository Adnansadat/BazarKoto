import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, catchError, map, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[];
}

export interface PagedResponse<T> {
  success: boolean;
  message: string;
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  errors: string[];
}

@Injectable({
  providedIn: 'root',
})
export class Api {
  private readonly baseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  constructor(private readonly http: HttpClient) {}

  get<T>(path: string, params?: Record<string, string | number | boolean | null | undefined>): Observable<T> {
    return this.http
      .get<ApiResponse<T>>(this.buildUrl(path), {
        headers: this.buildHeaders(),
        params: this.buildParams(params),
      })
      .pipe(
        map(response => this.unwrap(response)),
        catchError(error => this.handleError(error)),
      );
  }

  getPaged<T>(
    path: string,
    params?: Record<string, string | number | boolean | null | undefined>,
  ): Observable<PagedResponse<T>> {
    return this.http
      .get<PagedResponse<T>>(this.buildUrl(path), {
        headers: this.buildHeaders(),
        params: this.buildParams(params),
      })
      .pipe(
        map(response => this.unwrapPaged(response)),
        catchError(error => this.handleError(error)),
      );
  }

  getBlob(path: string): Observable<Blob> {
    return this.http
      .get(this.buildUrl(path), {
        headers: this.buildHeaders(),
        responseType: 'blob',
      })
      .pipe(catchError(error => this.handleError(error)));
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<ApiResponse<T>>(this.buildUrl(path), body, { headers: this.buildHeaders() })
      .pipe(
        map(response => this.unwrap(response)),
        catchError(error => this.handleError(error)),
      );
  }

  postResponse<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
    return this.http
      .post<ApiResponse<T>>(this.buildUrl(path), body, { headers: this.buildHeaders() })
      .pipe(catchError(error => this.handleError(error)));
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .put<ApiResponse<T>>(this.buildUrl(path), body, { headers: this.buildHeaders() })
      .pipe(
        map(response => this.unwrap(response)),
        catchError(error => this.handleError(error)),
      );
  }

  patch<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .patch<ApiResponse<T>>(this.buildUrl(path), body, { headers: this.buildHeaders() })
      .pipe(
        map(response => this.unwrap(response)),
        catchError(error => this.handleError(error)),
      );
  }

  delete<T>(path: string): Observable<T> {
    return this.http
      .delete<ApiResponse<T>>(this.buildUrl(path), { headers: this.buildHeaders() })
      .pipe(
        map(response => this.unwrap(response)),
        catchError(error => this.handleError(error)),
      );
  }

  private buildUrl(path: string): string {
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;
    return `${this.baseUrl}${normalizedPath}`;
  }

  private buildHeaders(): HttpHeaders {
    const token = localStorage.getItem('bazarKoto.accessToken');

    if (!token) {
      return new HttpHeaders();
    }

    const authorization = token.startsWith('Bearer ') ? token : `Bearer ${token}`;
    return new HttpHeaders({ Authorization: authorization });
  }

  private buildParams(params?: Record<string, string | number | boolean | null | undefined>): HttpParams {
    let httpParams = new HttpParams();

    for (const [key, value] of Object.entries(params ?? {})) {
      if (value !== null && value !== undefined && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    }

    return httpParams;
  }

  private unwrap<T>(response: ApiResponse<T>): T {
    if (!response.success) {
      throw new Error(response.errors?.join(', ') || response.message || 'Request failed.');
    }

    return response.data as T;
  }

  private unwrapPaged<T>(response: PagedResponse<T>): PagedResponse<T> {
    if (!response.success) {
      throw new Error(response.errors?.join(', ') || response.message || 'Request failed.');
    }

    return response;
  }

  private handleError(error: unknown): Observable<never> {
    const response = this.getErrorResponse(error);
    const message = response?.errors?.join(', ') || response?.message;

    if (message) {
      return throwError(() => new Error(message));
    }

    if (error instanceof Error) {
      return throwError(() => error);
    }

    return throwError(() => new Error('Request failed.'));
  }

  private getErrorResponse(error: unknown): ApiResponse<unknown> | null {
    if (
      typeof error === 'object' &&
      error !== null &&
      'error' in error &&
      typeof error.error === 'object' &&
      error.error !== null &&
      'success' in error.error
    ) {
      return error.error as ApiResponse<unknown>;
    }

    return null;
  }
}
