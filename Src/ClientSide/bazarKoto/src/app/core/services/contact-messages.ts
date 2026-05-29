import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';

interface ContactMessageSubmitApiResponse {
  success: boolean;
  message: string;
  id: string;
  errors?: string[];
}

export interface ContactMessageSubmitResponse {
  id: string;
  message: string;
}

export class ContactMessageSubmitError extends Error {
  constructor(
    message: string,
    readonly validationErrors: string[] = [],
  ) {
    super(message);
  }
}

@Injectable({
  providedIn: 'root',
})
export class ContactMessages {
  private readonly baseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  constructor(private readonly http: HttpClient) {}

  submitContactMessage(formData: FormData): Observable<ContactMessageSubmitResponse> {
    return this.http
      .post<ContactMessageSubmitApiResponse>(`${this.baseUrl}/ContactMessages`, formData)
      .pipe(
        map(response => {
          if (!response.success) {
            throw new ContactMessageSubmitError(response.message, response.errors ?? []);
          }

          return {
            id: response.id,
            message: response.message,
          };
        }),
        catchError(error => throwError(() => this.toSubmitError(error))),
      );
  }

  private toSubmitError(error: unknown): ContactMessageSubmitError {
    if (error instanceof ContactMessageSubmitError) {
      return error;
    }

    if (
      typeof error === 'object' &&
      error !== null &&
      'error' in error &&
      typeof error.error === 'object' &&
      error.error !== null
    ) {
      const response = error.error as { message?: string; errors?: string[] };
      return new ContactMessageSubmitError(response.message ?? 'Request failed.', response.errors ?? []);
    }

    return new ContactMessageSubmitError('Request failed.');
  }
}
