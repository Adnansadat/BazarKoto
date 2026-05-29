import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Api } from './api';

export interface LocationResponse {
  id: string;
  nameEn: string;
  nameBn: string;
  slug: string;
  bbsCode?: string | null;
  type?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class Locations {
  constructor(private readonly api: Api) {}

  getDivisions(search?: string): Observable<LocationResponse[]> {
    return this.api.get<LocationResponse[]>('/locations/divisions', { search });
  }

  getDistricts(divisionId: string, search?: string): Observable<LocationResponse[]> {
    return this.api.get<LocationResponse[]>('/locations/districts', { divisionId, search });
  }

  getUpazilas(districtId: string, search?: string): Observable<LocationResponse[]> {
    return this.api.get<LocationResponse[]>('/locations/upazilas', { districtId, search });
  }

  getUnionOrWards(upazilaId: string, search?: string): Observable<LocationResponse[]> {
    return this.api.get<LocationResponse[]>('/locations/unions-or-wards', { upazilaId, search });
  }
}
