import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Auth } from '../../../../core/services/auth';

@Component({
  selector: 'app-admin-login',
  imports: [CommonModule, FormsModule],
  standalone: true,
  templateUrl: './admin-login.component.html',
  styleUrl: './admin-login.component.scss',
})
export class AdminLoginComponent {
  email = '';
  password = '';
  isSubmitting = false;
  errorMessage = '';

  constructor(
    private readonly auth: Auth,
    private readonly router: Router
  ) {}

  login(): void {
    if (!this.email || !this.password || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        void this.router.navigate(['/admin/dashboard']);
      },
      error: error => {
        this.errorMessage = error instanceof Error ? error.message : 'Login failed.';
        this.isSubmitting = false;
      },
    });
  }
}
