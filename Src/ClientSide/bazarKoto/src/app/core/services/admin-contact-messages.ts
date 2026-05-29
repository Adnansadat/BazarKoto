import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Api, PagedResponse } from './api';

export interface AdminContactMessageListRequest {
  search?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface AdminContactMessageListItem {
  id: string;
  name: string;
  email: string;
  subject: string;
  messagePreview?: string;
  status: string;
  hasScreenshot: boolean;
  createdAt: string;
  readAt?: string | null;
  resolvedAt?: string | null;
}

export interface AdminContactMessageDetail extends AdminContactMessageListItem {
  message: string;
  adminNote?: string | null;
  screenshotUrl?: string | null;
  screenshotFileName?: string | null;
  screenshotOriginalFileName?: string | null;
  screenshotContentType?: string | null;
  screenshotSizeBytes?: number | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  browserName?: string | null;
  deviceType?: string | null;
  os?: string | null;
  updatedAt?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class AdminContactMessages {
  constructor(private readonly api: Api) {}

  getMessages(filters: AdminContactMessageListRequest): Observable<PagedResponse<AdminContactMessageListItem>> {
    return this.api.getPaged<AdminContactMessageListItem>('/Admin/ContactMessages', { ...filters });
  }

  getMessage(id: string): Observable<AdminContactMessageDetail> {
    return this.api.get<AdminContactMessageDetail>(`/Admin/ContactMessages/${id}`);
  }

  updateStatus(id: string, status: string): Observable<AdminContactMessageDetail> {
    return this.api.patch<AdminContactMessageDetail>(`/Admin/ContactMessages/${id}/status`, { status });
  }

  updateNote(id: string, adminNote: string): Observable<AdminContactMessageDetail> {
    return this.api.patch<AdminContactMessageDetail>(`/Admin/ContactMessages/${id}/note`, { adminNote });
  }

  getScreenshot(id: string): Observable<Blob> {
    return this.api.getBlob(`/Admin/ContactMessages/${id}/screenshot`);
  }
}
