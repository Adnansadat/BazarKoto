import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root',
})
export class Language {
  private readonly translate = inject(TranslateService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly supportedLanguages = ['en', 'bn'] as const;

  readonly defaultLanguage = 'en';

  initialize(): void {
    this.translate.addLangs([...this.supportedLanguages]);
    this.translate.setFallbackLang(this.defaultLanguage);

    const savedLanguage = this.isBrowser ? localStorage.getItem('language') : null;
    const browserLanguage = this.isBrowser ? this.translate.getBrowserLang() : null;
    const initialLanguage =
      this.isSupported(savedLanguage) ? savedLanguage :
      this.isSupported(browserLanguage) ? browserLanguage :
      this.defaultLanguage;

    this.translate.use(initialLanguage);
  }

  use(language: string): void {
    if (!this.isSupported(language)) {
      return;
    }

    if (this.isBrowser) {
      localStorage.setItem('language', language);
    }

    this.translate.use(language);
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.defaultLanguage;
  }

  private isSupported(language: string | null | undefined): language is typeof this.supportedLanguages[number] {
    return !!language && this.supportedLanguages.includes(language as typeof this.supportedLanguages[number]);
  }
}
