import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root',
})
export class Language {
  private readonly translate = inject(TranslateService);
  private readonly supportedLanguages = ['en', 'bn'] as const;

  readonly defaultLanguage = 'en';

  initialize(): void {
    this.translate.addLangs([...this.supportedLanguages]);
    this.translate.setFallbackLang(this.defaultLanguage);

    const savedLanguage = localStorage.getItem('language');
    const browserLanguage = this.translate.getBrowserLang();
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

    localStorage.setItem('language', language);
    this.translate.use(language);
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.defaultLanguage;
  }

  private isSupported(language: string | null | undefined): language is typeof this.supportedLanguages[number] {
    return !!language && this.supportedLanguages.includes(language as typeof this.supportedLanguages[number]);
  }
}
