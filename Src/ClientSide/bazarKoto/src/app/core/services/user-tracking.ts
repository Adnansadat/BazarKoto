import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';

export interface BrowserGpsSnapshot {
  gpsPermissionStatus: 'granted' | 'denied' | 'prompt' | 'unavailable' | 'error' | 'unknown';
  gpsLatitude?: number | null;
  gpsLongitude?: number | null;
  gpsAccuracyMeters?: number | null;
}

export interface LastKnownLocation {
  divisionId?: string | null;
  districtId?: string | null;
  upazilaId?: string | null;
  unionOrWardId?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class UserTracking {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly trackingGuidKey = 'bazarKoto.trackingGuid';
  private readonly gpsPermissionStatusKey = 'bazarKoto.gpsPermissionStatus';
  private readonly gpsLatitudeKey = 'bazarKoto.gpsLatitude';
  private readonly gpsLongitudeKey = 'bazarKoto.gpsLongitude';
  private readonly gpsAccuracyMetersKey = 'bazarKoto.gpsAccuracyMeters';
  private readonly lastDivisionIdKey = 'bazarKoto.lastDivisionId';
  private readonly lastDistrictIdKey = 'bazarKoto.lastDistrictId';
  private readonly lastUpazilaIdKey = 'bazarKoto.lastUpazilaId';
  private readonly lastUnionOrWardIdKey = 'bazarKoto.lastUnionOrWardId';
  private readonly guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

  getOrCreateTrackingGuid(): string {
    const existing = this.getTrackingGuid();

    if (existing) {
      return existing;
    }

    const trackingGuid = this.createGuid();
    this.saveTrackingGuid(trackingGuid);
    return trackingGuid;
  }

  getTrackingGuid(): string | null {
    const trackingGuid = this.safeGet(this.trackingGuidKey);
    return trackingGuid && this.isValidGuid(trackingGuid) ? trackingGuid : null;
  }

  saveTrackingGuid(trackingGuid: string | null | undefined): void {
    if (!trackingGuid || !this.isValidGuid(trackingGuid)) {
      return;
    }

    this.safeSet(this.trackingGuidKey, trackingGuid);
  }

  getBrowserGpsSnapshot(): BrowserGpsSnapshot {
    return {
      gpsPermissionStatus: this.toGpsPermissionStatus(this.safeGet(this.gpsPermissionStatusKey)),
      gpsLatitude: this.toNumberOrNull(this.safeGet(this.gpsLatitudeKey)),
      gpsLongitude: this.toNumberOrNull(this.safeGet(this.gpsLongitudeKey)),
      gpsAccuracyMeters: this.toNumberOrNull(this.safeGet(this.gpsAccuracyMetersKey)),
    };
  }

  requestBrowserLocation(): Promise<BrowserGpsSnapshot> {
    if (!this.isBrowser || !navigator.geolocation) {
      const snapshot: BrowserGpsSnapshot = { gpsPermissionStatus: 'unavailable' };
      this.saveGpsSnapshot(snapshot);
      return Promise.resolve(snapshot);
    }

    return new Promise(resolve => {
      navigator.geolocation.getCurrentPosition(
        position => {
          const snapshot: BrowserGpsSnapshot = {
            gpsPermissionStatus: 'granted',
            gpsLatitude: position.coords.latitude,
            gpsLongitude: position.coords.longitude,
            gpsAccuracyMeters: position.coords.accuracy,
          };
          this.saveGpsSnapshot(snapshot);
          resolve(snapshot);
        },
        error => {
          const snapshot: BrowserGpsSnapshot = {
            gpsPermissionStatus: this.toBrowserGpsErrorStatus(error),
          };
          this.saveGpsSnapshot(snapshot);
          resolve(snapshot);
        },
        {
          enableHighAccuracy: false,
          maximumAge: 300000,
          timeout: 10000,
        },
      );
    });
  }

  saveLastKnownLocation(location: LastKnownLocation): void {
    this.safeSetOrRemove(this.lastDivisionIdKey, location.divisionId);
    this.safeSetOrRemove(this.lastDistrictIdKey, location.districtId);
    this.safeSetOrRemove(this.lastUpazilaIdKey, location.upazilaId);
    this.safeSetOrRemove(this.lastUnionOrWardIdKey, location.unionOrWardId);
  }

  getLastKnownLocation(): LastKnownLocation {
    return {
      divisionId: this.safeGet(this.lastDivisionIdKey),
      districtId: this.safeGet(this.lastDistrictIdKey),
      upazilaId: this.safeGet(this.lastUpazilaIdKey),
      unionOrWardId: this.safeGet(this.lastUnionOrWardIdKey),
    };
  }

  clearLastKnownLocation(): void {
    this.safeRemove(this.lastDivisionIdKey);
    this.safeRemove(this.lastDistrictIdKey);
    this.safeRemove(this.lastUpazilaIdKey);
    this.safeRemove(this.lastUnionOrWardIdKey);
  }

  private createGuid(): string {
    if (globalThis.crypto?.randomUUID) {
      return globalThis.crypto.randomUUID();
    }

    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, marker => {
      const random = Math.random() * 16 | 0;
      const value = marker === 'x' ? random : (random & 0x3) | 0x8;
      return value.toString(16);
    });
  }

  private saveGpsSnapshot(snapshot: BrowserGpsSnapshot): void {
    this.safeSet(this.gpsPermissionStatusKey, snapshot.gpsPermissionStatus);
    this.safeSetOrRemove(this.gpsLatitudeKey, snapshot.gpsLatitude?.toString());
    this.safeSetOrRemove(this.gpsLongitudeKey, snapshot.gpsLongitude?.toString());
    this.safeSetOrRemove(this.gpsAccuracyMetersKey, snapshot.gpsAccuracyMeters?.toString());
  }

  private isValidGuid(value: string): boolean {
    return this.guidPattern.test(value);
  }

  private toGpsPermissionStatus(value: string | null): BrowserGpsSnapshot['gpsPermissionStatus'] {
    if (value === 'granted' || value === 'denied' || value === 'prompt' || value === 'unavailable' || value === 'error') {
      return value;
    }

    return 'unknown';
  }

  private toBrowserGpsErrorStatus(error: GeolocationPositionError): BrowserGpsSnapshot['gpsPermissionStatus'] {
    if (error.code === error.PERMISSION_DENIED) {
      return 'denied';
    }

    if (error.code === error.POSITION_UNAVAILABLE) {
      return 'unavailable';
    }

    return 'error';
  }

  private toNumberOrNull(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  private safeGet(key: string): string | null {
    if (!this.isBrowser) {
      return null;
    }

    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private safeSet(key: string, value: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      localStorage.setItem(key, value);
    } catch {
      // localStorage can fail in private browsing or restricted environments.
    }
  }

  private safeSetOrRemove(key: string, value: string | null | undefined): void {
    if (value) {
      this.safeSet(key, value);
      return;
    }

    this.safeRemove(key);
  }

  private safeRemove(key: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      localStorage.removeItem(key);
    } catch {
      // localStorage can fail in private browsing or restricted environments.
    }
  }
}
