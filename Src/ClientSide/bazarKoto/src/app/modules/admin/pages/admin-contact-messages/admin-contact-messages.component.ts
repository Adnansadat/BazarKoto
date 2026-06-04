import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnDestroy, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AdminContactMessageDetail,
  AdminContactMessageListItem,
  AdminContactMessages,
} from '../../../../core/services/admin-contact-messages';
import { PagedResponse } from '../../../../core/services/api';
import { Auth } from '../../../../core/services/auth';

@Component({
  selector: 'app-admin-contact-messages',
  imports: [CommonModule, FormsModule],
  standalone: true,
  templateUrl: './admin-contact-messages.component.html',
  styleUrl: './admin-contact-messages.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminContactMessagesComponent implements OnInit, OnDestroy {
  readonly statuses = ['', 'New', 'Read', 'InProgress', 'Resolved', 'Spam'];
  readonly pageSize = 4;
  search = signal('');
  status = signal('');
  dateFrom = signal('');
  dateTo = signal('');
  pageNumber = signal(1);
  totalPages = signal(0);
  totalCount = signal(0);
  messages = signal<AdminContactMessageListItem[]>([]);
  selectedMessage = signal<AdminContactMessageDetail | null>(null);
  selectedStatus = signal('');
  adminNote = signal('');
  isLoadingList = signal(false);
  isLoadingDetail = signal(false);
  isSavingStatus = signal(false);
  isSavingNote = signal(false);
  isLoggingOut = signal(false);
  listError = signal('');
  detailError = signal('');
  statusMessage = signal('');
  screenshotPreviewUrl = signal<string | null>(null);
  screenshotError = signal('');

  constructor(
    private readonly contactMessages: AdminContactMessages,
    private readonly auth: Auth,
    private readonly router: Router,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {}

  ngOnInit(): void {
    this.loadMessages();
  }

  ngOnDestroy(): void {
    this.clearScreenshotPreview();
  }

  loadMessages(pageNumber = this.pageNumber()): void {
    this.pageNumber.set(this.clampPage(pageNumber));
    this.isLoadingList.set(true);
    this.listError.set('');

    this.contactMessages.getMessages({
      search: this.search().trim() || undefined,
      status: this.status() || undefined,
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined,
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize,
    }).subscribe({
      next: response => this.bindMessageList(response),
      error: error => {
        this.listError.set(error instanceof Error ? error.message : 'Unable to load contact messages.');
        this.isLoadingList.set(false);
      },
    });
  }

  applyFilters(): void {
    this.loadMessages(1);
  }

  clearFilters(): void {
    this.search.set('');
    this.status.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
    this.loadMessages(1);
  }

  openMessage(message: AdminContactMessageListItem): void {
    this.isLoadingDetail.set(true);
    this.detailError.set('');
    this.statusMessage.set('');

    this.contactMessages.getMessage(message.id).subscribe({
      next: detail => {
        this.bindDetail(detail);
        this.isLoadingDetail.set(false);
        this.loadMessages(this.pageNumber());
      },
      error: error => {
        this.detailError.set(error instanceof Error ? error.message : 'Unable to load contact message.');
        this.isLoadingDetail.set(false);
      },
    });
  }

  saveStatus(): void {
    const selectedMessage = this.selectedMessage();
    const selectedStatus = this.selectedStatus();

    if (!selectedMessage || !selectedStatus || this.isSavingStatus()) {
      return;
    }

    this.isSavingStatus.set(true);
    this.detailError.set('');
    this.statusMessage.set('');

    this.contactMessages.updateStatus(selectedMessage.id, selectedStatus).subscribe({
      next: detail => {
        this.bindDetail(detail);
        this.statusMessage.set('Status updated.');
        this.isSavingStatus.set(false);
        this.loadMessages(this.pageNumber());
      },
      error: error => {
        this.detailError.set(error instanceof Error ? error.message : 'Unable to update status.');
        this.isSavingStatus.set(false);
      },
    });
  }

  saveNote(): void {
    const selectedMessage = this.selectedMessage();
    const adminNote = this.adminNote();

    if (!selectedMessage || this.isSavingNote() || adminNote.length > 1000) {
      return;
    }

    this.isSavingNote.set(true);
    this.detailError.set('');
    this.statusMessage.set('');

    this.contactMessages.updateNote(selectedMessage.id, adminNote).subscribe({
      next: detail => {
        this.bindDetail(detail);
        this.statusMessage.set('Admin note saved.');
        this.isSavingNote.set(false);
        this.loadMessages(this.pageNumber());
      },
      error: error => {
        this.detailError.set(error instanceof Error ? error.message : 'Unable to save admin note.');
        this.isSavingNote.set(false);
      },
    });
  }

  previousPage(): void {
    if (this.pageNumber() > 1) {
      this.loadMessages(this.pageNumber() - 1);
    }
  }

  nextPage(): void {
    if (this.pageNumber() < this.totalPages()) {
      this.loadMessages(this.pageNumber() + 1);
    }
  }

  firstPage(): void {
    if (this.pageNumber() > 1) {
      this.loadMessages(1);
    }
  }

  lastPage(): void {
    if (this.totalPages() > 1 && this.pageNumber() < this.totalPages()) {
      this.loadMessages(this.totalPages());
    }
  }

  screenshotUrl(message: AdminContactMessageDetail): string | null {
    return message.screenshotUrl ? this.screenshotPreviewUrl() : null;
  }

  replyUrl(message: AdminContactMessageDetail): string {
    return `mailto:${encodeURIComponent(message.email)}?subject=${encodeURIComponent(`Re: ${message.subject}`)}`;
  }

  logout(): void {
    if (this.isLoggingOut()) {
      return;
    }

    this.isLoggingOut.set(true);
    this.auth.logoutFromServer().subscribe({
      next: () => void this.router.navigate(['/admin']),
      error: () => {
        this.auth.logout();
        void this.router.navigate(['/admin']);
      },
    });
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString() : '-';
  }

  formatBytes(value?: number | null): string {
    if (!value) {
      return '-';
    }

    if (value < 1024) {
      return `${value} B`;
    }

    if (value < 1024 * 1024) {
      return `${(value / 1024).toFixed(1)} KB`;
    }

    return `${(value / (1024 * 1024)).toFixed(1)} MB`;
  }

  formatNumber(value: number): string {
    return value.toLocaleString();
  }

  private bindMessageList(response: PagedResponse<AdminContactMessageListItem>): void {
    this.messages.set(response.data);
    this.totalPages.set(response.totalPages);
    this.pageNumber.set(this.clampPage(response.pageNumber));
    this.totalCount.set(response.totalCount);
    this.isLoadingList.set(false);
  }

  private clampPage(pageNumber: number): number {
    const maxPage = Math.max(1, this.totalPages() || 1);
    return Math.min(Math.max(1, pageNumber), maxPage);
  }

  private bindDetail(detail: AdminContactMessageDetail): void {
    this.selectedMessage.set(detail);
    this.selectedStatus.set(detail.status);
    this.adminNote.set(detail.adminNote ?? '');
    this.loadScreenshotPreview(detail);
  }

  private loadScreenshotPreview(detail: AdminContactMessageDetail): void {
    this.clearScreenshotPreview();
    this.screenshotError.set('');

    if (!detail.screenshotUrl) {
      return;
    }

    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.contactMessages.getScreenshot(detail.id).subscribe({
      next: blob => {
        this.screenshotPreviewUrl.set(URL.createObjectURL(blob));
      },
      error: () => {
        this.screenshotError.set('Unable to load image preview.');
      },
    });
  }

  private clearScreenshotPreview(): void {
    const screenshotPreviewUrl = this.screenshotPreviewUrl();

    if (screenshotPreviewUrl && isPlatformBrowser(this.platformId)) {
      URL.revokeObjectURL(screenshotPreviewUrl);
    }

    this.screenshotPreviewUrl.set(null);
  }
}
