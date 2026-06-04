import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgFor, NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ContactMessageSubmitError, ContactMessages } from '../../core/services/contact-messages';
import { DraftService } from '../../core/services/draft';

type ContactField = 'name' | 'email' | 'subject' | 'message';

interface ContactFormDraft {
  name: string;
  email: string;
  subject: string;
  message: string;
}

@Component({
  selector: 'app-contact-page',
  standalone: true,
  imports: [TranslateModule, FormsModule, NgIf, NgFor],
  templateUrl: './contact-page.component.html',
  styleUrl: './contact-page.component.scss',
})
export class ContactPageComponent implements AfterViewInit, OnDestroy {
  @ViewChild('screenshotInput') private screenshotInput?: ElementRef<HTMLInputElement>;

  private readonly maxScreenshotSizeBytes = 3 * 1024 * 1024;
  private readonly allowedScreenshotTypes = new Set(['image/png', 'image/jpeg', 'image/webp']);
  private readonly draftKey = 'bazarKoto.contactFormDraft';
  private static savedScrollY: number | null = null;

  name = signal('');
  email = signal('');
  subject = signal('');
  message = signal('');
  selectedScreenshot = signal<File | null>(null);
  screenshotErrorKey = signal('');
  backendErrors: string[] = [];
  isSubmitting = false;
  errorMessageKey = '';
  showSuccessModal = false;
  touched: Record<ContactField, boolean> = {
    name: false,
    email: false,
    subject: false,
    message: false,
  };

  constructor(
    private readonly contactMessages: ContactMessages,
    private readonly draftService: DraftService
  ) {
    this.restoreDraft();
  }

  ngAfterViewInit(): void {
    if (ContactPageComponent.savedScrollY === null) {
      return;
    }

    const scrollY = ContactPageComponent.savedScrollY;
    requestAnimationFrame(() => window.scrollTo({ top: scrollY, behavior: 'auto' }));
  }

  ngOnDestroy(): void {
    ContactPageComponent.savedScrollY = window.scrollY;
  }

  submit(): void {
    if (this.isSubmitting) {
      return;
    }

    this.markAllTouched();
    this.errorMessageKey = '';
    this.backendErrors = [];

    if (!this.isFormValid()) {
      return;
    }

    this.isSubmitting = true;

    this.contactMessages.submitContactMessage(this.buildFormData())
      .pipe(finalize(() => {
        this.isSubmitting = false;
      }))
      .subscribe({
        next: () => {
          this.resetForm();
          this.clearDraft();
          this.showSuccessModal = true;
        },
        error: error => {
          this.errorMessageKey = 'contact.error.submit';
          this.backendErrors = error instanceof ContactMessageSubmitError
            ? error.validationErrors
            : [];
        },
      });
  }

  markTouched(field: ContactField): void {
    this.touched[field] = true;
  }

  saveDraft(): void {
    this.draftService.saveDraft(this.draftKey, {
      name: this.name(),
      email: this.email(),
      subject: this.subject(),
      message: this.message(),
    } satisfies ContactFormDraft);
  }

  getValidationKeys(field: ContactField): string[] {
    if (!this.touched[field]) {
      return [];
    }

    const value = this.getFieldValue(field).trim();

    if (!value) {
      return [`contact.validation.${field}.required`];
    }

    if (field === 'name') {
      return this.lengthValidationKeys(field, value, 2, 80);
    }

    if (field === 'email') {
      if (value.length > 120) {
        return ['contact.validation.email.max'];
      }

      return this.isValidEmail(value) ? [] : ['contact.validation.email.invalid'];
    }

    if (field === 'subject') {
      return this.lengthValidationKeys(field, value, 5, 150);
    }

    return this.lengthValidationKeys(field, value, 20, 2000);
  }

  onScreenshotSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    const file = files?.[0] ?? null;

    this.screenshotErrorKey.set('');
    this.selectedScreenshot.set(null);

    if (!file) {
      return;
    }

    if ((files?.length ?? 0) > 1) {
      this.screenshotErrorKey.set('contact.validation.screenshot.single');
      input.value = '';
      return;
    }

    if (!this.allowedScreenshotTypes.has(file.type)) {
      this.screenshotErrorKey.set('contact.validation.screenshot.type');
      input.value = '';
      return;
    }

    if (file.size > this.maxScreenshotSizeBytes) {
      this.screenshotErrorKey.set('contact.validation.screenshot.max');
      input.value = '';
      return;
    }

    this.selectedScreenshot.set(file);
  }

  removeScreenshot(input: HTMLInputElement): void {
    this.selectedScreenshot.set(null);
    this.screenshotErrorKey.set('');
    input.value = '';
  }

  closeSuccessModal(): void {
    this.showSuccessModal = false;
  }

  private buildFormData(): FormData {
    const formData = new FormData();
    formData.append('name', this.name().trim());
    formData.append('email', this.email().trim());
    formData.append('subject', this.subject().trim());
    formData.append('message', this.message().trim());

    const selectedScreenshot = this.selectedScreenshot();
    if (selectedScreenshot) {
      formData.append('screenshot', selectedScreenshot);
    }

    return formData;
  }

  private isFormValid(): boolean {
    return (Object.keys(this.touched) as ContactField[])
      .every(field => this.getValidationKeys(field).length === 0)
      && !this.screenshotErrorKey();
  }

  private markAllTouched(): void {
    this.touched = {
      name: true,
      email: true,
      subject: true,
      message: true,
    };
  }

  private resetForm(): void {
    this.name.set('');
    this.email.set('');
    this.subject.set('');
    this.message.set('');
    this.selectedScreenshot.set(null);
    this.screenshotErrorKey.set('');
    this.backendErrors = [];
    this.errorMessageKey = '';
    this.clearScreenshotInput();
    this.touched = {
      name: false,
      email: false,
      subject: false,
      message: false,
    };
  }

  private restoreDraft(): void {
    const draft = this.draftService.getDraft<ContactFormDraft>(this.draftKey);

    if (!draft) {
      return;
    }

    this.name.set(draft.name ?? '');
    this.email.set(draft.email ?? '');
    this.subject.set(draft.subject ?? '');
    this.message.set(draft.message ?? '');
  }

  private clearDraft(): void {
    this.draftService.clearDraft(this.draftKey);
  }

  private lengthValidationKeys(
    field: ContactField,
    value: string,
    minLength: number,
    maxLength: number,
  ): string[] {
    if (value.length < minLength) {
      return [`contact.validation.${field}.min`];
    }

    if (value.length > maxLength) {
      return [`contact.validation.${field}.max`];
    }

    return [];
  }

  private isValidEmail(value: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
  }

  private getFieldValue(field: ContactField): string {
    return {
      name: this.name(),
      email: this.email(),
      subject: this.subject(),
      message: this.message(),
    }[field];
  }

  private clearScreenshotInput(): void {
    if (this.screenshotInput) {
      this.screenshotInput.nativeElement.value = '';
    }
  }
}
