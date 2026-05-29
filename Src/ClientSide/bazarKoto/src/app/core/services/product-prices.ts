import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Api, PagedResponse } from './api';

export interface PublicProductPriceSearchRequest {
  divisionId?: string;
  districtId?: string;
  upazilaId?: string;
  unionOrWardId?: string;
  marketId?: string;
  categoryId?: string;
  productId?: string;
  date?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface PublicProductPriceResponse {
  id: string;
  productId: string;
  productNameEn: string;
  productNameBn: string;
  categoryId: string;
  categoryNameEn: string;
  categoryNameBn: string;
  marketId: string;
  marketName: string;
  divisionId?: string | null;
  divisionNameEn: string;
  divisionNameBn: string;
  districtId?: string | null;
  districtNameEn: string;
  districtNameBn: string;
  upazilaId?: string | null;
  upazilaNameEn: string;
  upazilaNameBn: string;
  unionOrWardId?: string | null;
  unionOrWardNameEn?: string | null;
  unionOrWardNameBn?: string | null;
  unit: string;
  pricePerUnit: number;
  quantityChecked?: number | null;
  priceDate: string;
  priceTime?: string | null;
  sellerType: string;
  priceSource: string;
  qualityGrade: string;
  notes?: string | null;
  status: string;
}

@Injectable({
  providedIn: 'root',
})
export class ProductPrices {
  constructor(private readonly api: Api) {}

  getPublicProductPrices(
    filters: PublicProductPriceSearchRequest = {},
  ): Observable<PagedResponse<PublicProductPriceResponse>> {
    return this.api.getPaged<PublicProductPriceResponse>('/ProductPrices', { ...filters });
  }
}
