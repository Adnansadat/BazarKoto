import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Language } from '../../../core/services/language';

@Component({
  selector: 'app-language-switcher',
  imports: [],
  templateUrl: './language-switcher.html',
  styleUrl: './language-switcher.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSwitcher {
  constructor(protected readonly language: Language) {}

  switchTo(language: string): void {
    this.language.use(language);
  }
}
