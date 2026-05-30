import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Api, PagedResponse } from './api';

export interface AdminPriceListRequest {
  search?: string;
  status?: string;
  pageNumber?: number;
  pageSize?: number;
  dateFrom?: string;
  dateTo?: string;
  productId?: string;
  marketId?: string;
}

export interface AdminPriceRecord {
  id: string;
  productId: string;
  productName: string;
  productLocalName?: string | null;
  alternateName?: string | null;
  marketId: string;
  marketName: string;
  location: string;
  price: number;
  unit: string;
  submittedAt: string;
  status: string;
  source: string;
  updatedAt?: string | null;
}

export interface AdminPriceListResponse extends PagedResponse<AdminPriceRecord> {
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AdminUpdatePriceRequest {
  price: number;
  unit: string;
  status: string;
  source: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdminPrices {
  constructor(private readonly api: Api) {}

  getAdminPrices(query: AdminPriceListRequest): Observable<AdminPriceListResponse> {
    return this.api.getPaged<AdminPriceRecord>('/Admin/prices', { ...query }) as Observable<AdminPriceListResponse>;
  }

  updateAdminPrice(id: string, request: AdminUpdatePriceRequest): Observable<AdminPriceRecord> {
    return this.api.put<AdminPriceRecord>(`/Admin/prices/${id}`, request);
  }
}
