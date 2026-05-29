import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of, timeout } from 'rxjs';

export interface ResolvedApproximateLocation {
  divisionName?: string | null;
  districtName?: string | null;
  upazilaName?: string | null;
  unionOrWardName?: string | null;
  rawAddress?: string | null;
  provider: string;
  confidence: 'low' | 'medium' | 'high';
}

interface NominatimReverseResponse {
  display_name?: string;
  address?: {
    state?: string;
    city?: string;
    county?: string;
    municipality?: string;
    town?: string;
    village?: string;
    suburb?: string;
    city_district?: string;
    state_district?: string;
    district?: string;
    neighbourhood?: string;
  };
}

@Injectable({
  providedIn: 'root',
})
export class LocationResolver {
  private readonly reverseGeocodeUrl = 'https://nominatim.openstreetmap.org/reverse';

  constructor(private readonly http: HttpClient) {}

  reverseGeocode(latitude: number, longitude: number): Observable<ResolvedApproximateLocation | null> {
    const params = new HttpParams()
      .set('format', 'jsonv2')
      .set('lat', latitude)
      .set('lon', longitude)
      .set('zoom', '12')
      .set('addressdetails', '1')
      .set('accept-language', 'en,bn');

    // TODO: Move provider selection to backend/config when usage grows or needs a paid SLA/key.
    return this.http.get<NominatimReverseResponse>(this.reverseGeocodeUrl, { params }).pipe(
      timeout(8000),
      map(response => this.toApproximateLocation(response)),
      catchError(() => of(null)),
    );
  }

  private toApproximateLocation(response: NominatimReverseResponse | null): ResolvedApproximateLocation | null {
    if (!response?.address) {
      return null;
    }

    const address = response.address;
    const districtName = address.state_district ?? address.county ?? address.city ?? address.district ?? null;
    const upazilaName = address.municipality ?? address.town ?? address.city_district ?? address.suburb ?? address.village ?? null;
    const unionOrWardName = address.village ?? address.neighbourhood ?? address.suburb ?? null;

    return {
      divisionName: address.state ?? null,
      districtName,
      upazilaName,
      unionOrWardName,
      rawAddress: response.display_name ?? null,
      provider: 'OpenStreetMap Nominatim',
      confidence: this.toConfidence(address.state, districtName, upazilaName),
    };
  }

  private toConfidence(
    divisionName: string | null | undefined,
    districtName: string | null | undefined,
    upazilaName: string | null | undefined,
  ): ResolvedApproximateLocation['confidence'] {
    if (divisionName && districtName && upazilaName) {
      return 'high';
    }

    if (divisionName && districtName) {
      return 'medium';
    }

    return 'low';
  }
}
