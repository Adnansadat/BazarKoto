import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
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
})
export class AdminContactMessagesComponent implements OnInit, OnDestroy {
  readonly statuses = ['', 'New', 'Read', 'InProgress', 'Resolved', 'Spam'];
  readonly pageSize = 4;
  search = '';
  status = '';
  dateFrom = '';
  dateTo = '';
  pageNumber = 1;
  totalPages = 0;
  totalCount = 0;
  messages: AdminContactMessageListItem[] = [];
  selectedMessage: AdminContactMessageDetail | null = null;
  selectedStatus = '';
  adminNote = '';
  isLoadingList = false;
  isLoadingDetail = false;
  isSavingStatus = false;
  isSavingNote = false;
  isLoggingOut = false;
  listError = '';
  detailError = '';
  statusMessage = '';
  screenshotPreviewUrl: string | null = null;
  screenshotError = '';

  constructor(
    private readonly contactMessages: AdminContactMessages,
    private readonly auth: Auth,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadMessages();
  }

  ngOnDestroy(): void {
    this.clearScreenshotPreview();
  }

  loadMessages(pageNumber = this.pageNumber): void {
    this.pageNumber = this.clampPage(pageNumber);
    this.isLoadingList = true;
    this.listError = '';

    this.contactMessages.getMessages({
      search: this.search.trim() || undefined,
      status: this.status || undefined,
      dateFrom: this.dateFrom || undefined,
      dateTo: this.dateTo || undefined,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
    }).subscribe({
      next: response => this.bindMessageList(response),
      error: error => {
        this.listError = error instanceof Error ? error.message : 'Unable to load contact messages.';
        this.isLoadingList = false;
      },
    });
  }

  applyFilters(): void {
    this.loadMessages(1);
  }

  clearFilters(): void {
    this.search = '';
    this.status = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.loadMessages(1);
  }

  openMessage(message: AdminContactMessageListItem): void {
    this.isLoadingDetail = true;
    this.detailError = '';
    this.statusMessage = '';

    this.contactMessages.getMessage(message.id).subscribe({
      next: detail => {
        this.bindDetail(detail);
        this.isLoadingDetail = false;
        this.loadMessages(this.pageNumber);
      },
      error: error => {
        this.detailError = error instanceof Error ? error.message : 'Unable to load contact message.';
        this.isLoadingDetail = false;
      },
    });
  }

  saveStatus(): void {
    if (!this.selectedMessage || !this.selectedStatus || this.isSavingStatus) {
      return;
    }

    this.isSavingStatus = true;
    this.detailError = '';
    this.statusMessage = '';

    this.contactMessages.updateStatus(this.selectedMessage.id, this.selectedStatus).subscribe({
      next: detail => {
        this.bindDetail(detail);
        this.statusMessage = 'Status updated.';
        this.isSavingStatus = false;
        this.loadMessages(this.pageNumber);
      },
      error: error => {
        this.detailError = error instanceof Error ? error.message : 'Unable to update status.';
        this.isSavingStatus = false;
      },
    });
  }

  saveNote(): void {
    if (!this.selectedMessage || this.isSavingNote || this.adminNote.length > 1000) {
      return;
    }

    this.isSavingNote = true;
    this.detailError = '';
    this.statusMessage = '';

    this.contactMessages.updateNote(this.selectedMessage.id, this.adminNote).subscribe({
      next: detail => {
        this.bindDetail(detail);
        this.statusMessage = 'Admin note saved.';
        this.isSavingNote = false;
        this.loadMessages(this.pageNumber);
      },
      error: error => {
        this.detailError = error instanceof Error ? error.message : 'Unable to save admin note.';
        this.isSavingNote = false;
      },
    });
  }

  previousPage(): void {
    if (this.pageNumber > 1) {
      this.loadMessages(this.pageNumber - 1);
    }
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.loadMessages(this.pageNumber + 1);
    }
  }

  firstPage(): void {
    if (this.pageNumber > 1) {
      this.loadMessages(1);
    }
  }

  lastPage(): void {
    if (this.totalPages > 1 && this.pageNumber < this.totalPages) {
      this.loadMessages(this.totalPages);
    }
  }

  screenshotUrl(message: AdminContactMessageDetail): string | null {
    return message.screenshotUrl ? this.screenshotPreviewUrl : null;
  }

  replyUrl(message: AdminContactMessageDetail): string {
    return `mailto:${encodeURIComponent(message.email)}?subject=${encodeURIComponent(`Re: ${message.subject}`)}`;
  }

  logout(): void {
    if (this.isLoggingOut) {
      return;
    }

    this.isLoggingOut = true;
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
    this.messages = response.data;
    this.totalPages = response.totalPages;
    this.pageNumber = this.clampPage(response.pageNumber);
    this.totalCount = response.totalCount;
    this.isLoadingList = false;
  }

  private clampPage(pageNumber: number): number {
    const maxPage = Math.max(1, this.totalPages || 1);
    return Math.min(Math.max(1, pageNumber), maxPage);
  }

  private bindDetail(detail: AdminContactMessageDetail): void {
    this.selectedMessage = detail;
    this.selectedStatus = detail.status;
    this.adminNote = detail.adminNote ?? '';
    this.loadScreenshotPreview(detail);
  }

  private loadScreenshotPreview(detail: AdminContactMessageDetail): void {
    this.clearScreenshotPreview();
    this.screenshotError = '';

    if (!detail.screenshotUrl) {
      return;
    }

    this.contactMessages.getScreenshot(detail.id).subscribe({
      next: blob => {
        this.screenshotPreviewUrl = URL.createObjectURL(blob);
      },
      error: () => {
        this.screenshotError = 'Unable to load image preview.';
      },
    });
  }

  private clearScreenshotPreview(): void {
    if (this.screenshotPreviewUrl) {
      URL.revokeObjectURL(this.screenshotPreviewUrl);
      this.screenshotPreviewUrl = null;
    }
  }
}
