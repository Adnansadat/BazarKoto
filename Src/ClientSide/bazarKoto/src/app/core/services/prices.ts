import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Api } from './api';

export interface SubmitPriceRequest {
  marketId: string;
  productId: string;
  unit: string;
  pricePerUnit: number;
  quantityChecked?: number | null;
  priceDate: string;
  priceTime?: string | null;
  sellerType: string;
  priceSource: string;
  qualityGrade: string;
  notes?: string | null;
  trackingGuid?: string | null;
  gpsLatitude?: number | null;
  gpsLongitude?: number | null;
  gpsAccuracyMeters?: number | null;
  gpsPermissionStatus?: string | null;
  ipBasedCountry?: string | null;
  ipBasedRegion?: string | null;
  ipBasedCity?: string | null;
  ipBasedLatitude?: number | null;
  ipBasedLongitude?: number | null;
  ipLocationProvider?: string | null;
  ipLocationAccuracy?: string | null;
  locationSource?: string | null;
}

export interface PriceSubmissionResponse {
  id: string;
  marketId: string;
  marketName: string;
  productId: string;
  productNameEn: string;
  productNameBn: string;
  categoryId: string;
  categoryNameEn: string;
  categoryNameBn: string;
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
  trackingGuid?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class Prices {
  constructor(private readonly api: Api) {}

  submitPrice(request: SubmitPriceRequest): Observable<PriceSubmissionResponse> {
    return this.api.post<PriceSubmissionResponse>('/Prices', request);
  }
}
