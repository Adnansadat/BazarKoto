import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-contact-page',
  standalone: true,
  imports: [TranslateModule, FormsModule, NgIf],
  templateUrl: './contact-page.component.html',
  styleUrl: './contact-page.component.scss',
})
export class ContactPageComponent {
  name = '';
  email = '';
  subject = '';
  message = '';
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';

  constructor(private readonly api: Api) {}

  submit(): void {
    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.api.post('/Contact', {
      name: this.name,
      email: this.email,
      subject: this.subject,
      message: this.message,
    }).subscribe({
      next: () => {
        this.successMessage = 'Message sent successfully.';
        this.name = '';
        this.email = '';
        this.subject = '';
        this.message = '';
        this.isSubmitting = false;
      },
      error: error => {
        this.errorMessage = error instanceof Error ? error.message : 'Unable to send message.';
        this.isSubmitting = false;
      },
    });
  }
}
