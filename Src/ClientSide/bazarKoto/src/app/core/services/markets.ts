import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Api, PagedResponse } from './api';

export interface MarketOptionSearchRequest {
  search?: string;
  divisionId?: string;
  districtId?: string;
  upazilaId?: string;
  unionOrWardId?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface MarketOptionResponse {
  id: string;
  marketId: string;
  marketName: string;
  displayLabel: string;
  divisionId: string;
  divisionNameEn: string;
  divisionNameBn: string;
  districtId: string;
  districtNameEn: string;
  districtNameBn: string;
  upazilaId: string;
  upazilaNameEn: string;
  upazilaNameBn: string;
  unionOrWardId?: string | null;
  unionOrWardNameEn?: string | null;
  unionOrWardNameBn?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class Markets {
  constructor(private readonly api: Api) {}

  getMarketOptions(
    filters: MarketOptionSearchRequest = {},
  ): Observable<PagedResponse<MarketOptionResponse>> {
    return this.api.getPaged<MarketOptionResponse>('/Markets/options', { ...filters });
  }
}
