import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe, NgIf } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageSwitcher } from '../language-switcher/language-switcher';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSwitcher, NgIf, AsyncPipe],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  readonly isAdmin$;
  private secretClickCount = 0;
  private lastSecretClick = 0;

  constructor(
    private readonly router: Router,
    private readonly auth: Auth
  ) {
    this.isAdmin$ = this.auth.isAdmin$;
  }

  handleBrandClick(event: MouseEvent): void {
    const now = Date.now();

    this.secretClickCount = now - this.lastSecretClick > 1600 ? 1 : this.secretClickCount + 1;
    this.lastSecretClick = now;

    if (this.secretClickCount < 5) {
      return;
    }

    event.preventDefault();
    this.secretClickCount = 0;
    this.router.navigate(['/admin']);
  }
}
