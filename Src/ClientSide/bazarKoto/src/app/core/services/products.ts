import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Api, PagedResponse } from './api';

export interface ProductOptionSearchRequest {
  search?: string;
  categoryId?: string;
  unionOrWardId?: string;
  marketId?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface ProductOptionResponse {
  id: string;
  productId: string;
  productNameEn: string;
  productNameBn: string;
  localOrAlternateName?: string | null;
  categoryId: string;
  categoryNameEn: string;
  categoryNameBn: string;
  primaryUnit: string;
  productState?: string;
  displayLabel: string;
}

@Injectable({
  providedIn: 'root',
})
export class Products {
  constructor(private readonly api: Api) {}

  getProductOptions(
    filters: ProductOptionSearchRequest = {},
  ): Observable<PagedResponse<ProductOptionResponse>> {
    return this.api.getPaged<ProductOptionResponse>('/Products/options', { ...filters });
  }
}
