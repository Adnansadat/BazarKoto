import { Component, inject } from '@angular/core';
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

  constructor() {
    this.language.initialize();
    this.trackPageVisits();
  }

  private trackPageVisits(): void {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
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

    const visitorId = crypto.randomUUID();
    localStorage.setItem(key, visitorId);
    return visitorId;
  }
}
