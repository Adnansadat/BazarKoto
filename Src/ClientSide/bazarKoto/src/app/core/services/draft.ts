import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DraftService {
  saveDraft(key: string, data: unknown): void {
    try {
      localStorage.setItem(key, JSON.stringify(data));
    } catch {
      // localStorage can fail in private browsing or restricted environments.
    }
  }

  getDraft<T>(key: string): T | null {
    try {
      const rawDraft = localStorage.getItem(key);
      return rawDraft ? JSON.parse(rawDraft) as T : null;
    } catch {
      this.clearDraft(key);
      return null;
    }
  }

  clearDraft(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch {
      // Ignore storage cleanup failures.
    }
  }

  hasDraft(key: string): boolean {
    try {
      return localStorage.getItem(key) !== null;
    } catch {
      return false;
    }
  }
}
