import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Auth } from '../../../../core/services/auth';

@Component({
  selector: 'app-admin-login',
  imports: [CommonModule, FormsModule],
  standalone: true,
  templateUrl: './admin-login.component.html',
  styleUrl: './admin-login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLoginComponent {
  email = signal('');
  password = signal('');
  isSubmitting = signal(false);
  errorMessage = signal('');

  constructor(
    private readonly auth: Auth,
    private readonly router: Router
  ) {}

  login(): void {
    if (!this.email() || !this.password() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.auth.login(this.email(), this.password()).subscribe({
      next: () => {
        void this.router.navigate(['/admin/dashboard']);
      },
      error: error => {
        this.errorMessage.set(error instanceof Error ? error.message : 'Login failed.');
        this.isSubmitting.set(false);
      },
    });
  }
}
