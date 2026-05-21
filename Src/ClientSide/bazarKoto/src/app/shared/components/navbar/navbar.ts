import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageSwitcher } from '../language-switcher/language-switcher';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSwitcher],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  private secretClickCount = 0;
  private lastSecretClick = 0;

  constructor(private readonly router: Router) {}

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
