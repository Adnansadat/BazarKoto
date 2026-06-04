import { isPlatformBrowser } from '@angular/common';
import { Component, DestroyRef, PLATFORM_ID, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { Navbar } from "./shared/components/navbar/navbar";
import { Footer } from "./shared/components/footer/footer";
import { Language } from './core/services/language';
import { Api } from './core/services/api';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Footer],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly language = inject(Language);
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  constructor() {
    this.language.initialize();
    this.trackPageVisits();
  }

  private trackPageVisits(): void {
    if (!this.isBrowser) {
      return;
    }

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(event => {
        this.api.post('/Analytics/page-visit', {
          path: event.urlAfterRedirects,
          pageTitle: document.title,
          visitorId: this.getVisitorId(),
          referrer: document.referrer || null,
          deviceType: window.innerWidth < 768 ? 'Mobile' : 'Desktop',
          country: 'Bangladesh',
        }).subscribe({ error: () => undefined });
      });
  }

  private getVisitorId(): string {
    const key = 'bazarKoto.visitorId';
    const existing = localStorage.getItem(key);

    if (existing) {
      return existing;
    }

    const visitorId = globalThis.crypto?.randomUUID?.() ?? this.createVisitorId();
    localStorage.setItem(key, visitorId);
    return visitorId;
  }

  private createVisitorId(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, marker => {
      const random = Math.random() * 16 | 0;
      const value = marker === 'x' ? random : (random & 0x3) | 0x8;
      return value.toString(16);
    });
  }
}
