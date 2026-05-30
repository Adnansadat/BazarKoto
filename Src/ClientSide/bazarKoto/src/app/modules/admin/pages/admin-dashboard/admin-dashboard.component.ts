import { CommonModule } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { AdminPriceRecord, AdminPrices } from '../../../../core/services/admin-prices';
import { Api } from '../../../../core/services/api';
import { Auth } from '../../../../core/services/auth';

interface DashboardMetric {
  label: string;
  value: string;
  helper: string;
  trend: string;
}

interface AdminQueueItem {
  label: string;
  count: number;
  helper: string;
  route?: string;
}

interface PeakHour {
  time: string;
  visits: string;
  share: string;
}

interface ManagementArea {
  title: string;
  description: string;
}

interface PriceEditForm {
  price: number | null;
  unit: string;
  status: string;
  source: string;
}

interface AdminDashboardResponse {
  traffic: {
    totalVisits: number;
    uniqueVisitors: number;
    uniqueVisitorsToday: number;
    todayVisits: number;
    thisWeekVisits: number;
    thisMonthVisits: number;
  };
  records: {
    totalMarkets: number;
    totalProducts: number;
    totalCategories: number;
    totalPriceSubmissions: number;
    totalContributors: number;
  };
  moderation: {
    pendingMarkets: number;
    pendingProducts: number;
    pendingPriceSubmissions: number;
    flaggedPriceSubmissions: number;
    pendingContactMessages: number;
  };
  peakHours: Array<{
    hour: number;
    visitCount: number;
  }>;
}

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule, FormsModule],
  standalone: true,
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  readonly peakHoursPageSize = 5;
  isLoading = true;
  isLoggingOut = false;
  isExportingTrafficReport = false;
  isPriceManagementModalOpen = false;
  errorMessage = '';
  exportMessage = '';
  exportErrorMessage = '';
  priceManagementMessage = '';
  priceManagementSearch = '';
  priceManagementStatus = '';
  priceManagementPage = 1;
  priceManagementTotalCount = 0;
  priceManagementTotalPages = 0;
  priceManagementHasPreviousPage = false;
  priceManagementHasNextPage = false;
  readonly priceManagementPageSize = 10;
  readonly priceManagementStatuses = ['Approved', 'Pending', 'Flagged', 'Rejected'];
  readonly priceSourceOptions = ['ObservedInMarket', 'SellerProvided', 'Receipt', 'UserReported', 'OnlineListing', 'Other'];
  readonly unitOptions = [
    { value: 'kg', label: 'kg' },
    { value: 'gram', label: 'gram' },
    { value: 'piece', label: 'piece' },
    { value: 'dozen', label: 'dozen' },
    { value: 'litre', label: 'litre' },
    { value: 'packet', label: 'packet' },
  ];
  isLoadingPriceRecords = false;
  isPriceEditModalOpen = false;
  isSavingPriceEdit = false;
  priceRecordsErrorMessage = '';
  priceEditMessage = '';
  priceEditErrorMessage = '';
  priceEditValidationErrors: string[] = [];
  selectedPriceRecord: AdminPriceRecord | null = null;
  priceEditForm: PriceEditForm = {
    price: null,
    unit: '',
    status: '',
    source: '',
  };
  priceRecords: AdminPriceRecord[] = [];
  trafficMetrics: DashboardMetric[] = [];
  dataMetrics: DashboardMetric[] = [];
  peakHours: PeakHour[] = [];
  peakHoursPage = 1;
  moderationQueue: AdminQueueItem[] = [];
  private priceRecordsRequest?: Subscription;
  private priceRecordsRequestId = 0;
  private priceSearchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  readonly managementAreas: ManagementArea[] = [
    {
      title: 'Price Management',
      description: 'Review, search, and update submitted product price records from one admin workspace.',
    },
  ];

  constructor(
    private readonly api: Api,
    private readonly adminPrices: AdminPrices,
    private readonly auth: Auth,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  ngOnDestroy(): void {
    this.priceRecordsRequest?.unsubscribe();

    if (this.priceSearchDebounceTimer) {
      clearTimeout(this.priceSearchDebounceTimer);
    }
  }

  private loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.api.get<AdminDashboardResponse>('/Admin/dashboard').subscribe({
      next: dashboard => {
        this.bindDashboard(dashboard);
        this.isLoading = false;
      },
      error: error => {
        this.handleLoadError(error);
      },
    });
  }

  private bindDashboard(dashboard: AdminDashboardResponse): void {
    const peakHour = dashboard.peakHours[0];
    this.peakHoursPage = 1;

    this.trafficMetrics = [
      {
        label: 'Total traffic',
        value: this.formatNumber(dashboard.traffic.totalVisits),
        helper: 'All tracked website visits',
        trend: `${this.formatNumber(dashboard.traffic.thisMonthVisits)} visits this month`,
      },
      {
        label: 'Today’s visitors',
        value: this.formatNumber(dashboard.traffic.todayVisits),
        helper: 'Website visits so far today',
        trend: `${this.formatNumber(dashboard.traffic.uniqueVisitorsToday)} unique visitors today`,
      },
      {
        label: 'Peak hour',
        value: peakHour ? this.formatHour(peakHour.hour) : '0',
        helper: 'Highest traffic window',
        trend: peakHour ? `${this.formatNumber(peakHour.visitCount)} visits` : '0 visits',
      },
      {
        label: 'Weekly traffic',
        value: this.formatNumber(dashboard.traffic.thisWeekVisits),
        helper: 'Tracked visits this week',
        trend: 'Live backend data',
      },
    ];

    this.dataMetrics = [
      {
        label: 'Markets',
        value: this.formatNumber(dashboard.records.totalMarkets),
        helper: 'Local market records',
        trend: `${this.formatNumber(dashboard.moderation.pendingMarkets)} need review`,
      },
      {
        label: 'Products',
        value: this.formatNumber(dashboard.records.totalProducts),
        helper: 'Clean catalog entries',
        trend: `${this.formatNumber(dashboard.moderation.pendingProducts)} pending`,
      },
      {
        label: 'Prices',
        value: this.formatNumber(dashboard.records.totalPriceSubmissions),
        helper: 'Submitted bazar prices',
        trend: `${this.formatNumber(dashboard.moderation.pendingPriceSubmissions)} pending`,
      },
      {
        label: 'Contributors',
        value: this.formatNumber(dashboard.records.totalContributors),
        helper: 'Community submitters',
        trend: `${this.formatNumber(dashboard.records.totalCategories)} product categories`,
      },
    ];

    this.peakHours = dashboard.peakHours.map(hour => ({
      time: this.formatHour(hour.hour),
      visits: this.formatNumber(hour.visitCount),
      share: 'Backend traffic data',
    }));

    this.moderationQueue = [
      {
        label: 'User support messages',
        count: dashboard.moderation.pendingContactMessages,
        helper: 'Contact and correction requests',
        route: '/admin/messages',
      },
      {
        label: 'Pending market approvals',
        count: dashboard.moderation.pendingMarkets,
        helper: 'New or edited market records',
      },
      {
        label: 'Pending product reviews',
        count: dashboard.moderation.pendingProducts,
        helper: 'Product records waiting for review',
      },
      {
        label: 'Flagged price submissions',
        count: dashboard.moderation.flaggedPriceSubmissions,
        helper: 'Outlier prices needing validation',
      },
    ];
  }

  openQueueItem(item: AdminQueueItem): void {
    if (item.route) {
      void this.router.navigate([item.route]);
    }
  }

  openPriceManagementModal(): void {
    this.isPriceManagementModalOpen = true;
    this.priceManagementMessage = '';
    this.priceManagementPage = 1;
    this.loadAdminPriceRecords();
  }

  closePriceManagementModal(): void {
    if (this.isSavingPriceEdit) {
      return;
    }

    this.priceRecordsRequest?.unsubscribe();
    this.closePriceEditModal();
    this.isPriceManagementModalOpen = false;
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.isSavingPriceEdit) {
      return;
    }

    if (this.isPriceEditModalOpen) {
      this.closePriceEditModal();
      return;
    }

    if (this.isPriceManagementModalOpen) {
      this.closePriceManagementModal();
    }
  }

  get priceManagementPageCount(): number {
    return Math.max(1, this.priceManagementTotalPages);
  }

  get priceManagementPageStart(): number {
    return this.priceManagementTotalCount === 0
      ? 0
      : (this.priceManagementPage - 1) * this.priceManagementPageSize + 1;
  }

  get priceManagementPageEnd(): number {
    return Math.min(this.priceManagementPage * this.priceManagementPageSize, this.priceManagementTotalCount);
  }

  onPriceManagementFiltersChange(): void {
    this.priceManagementPage = 1;
    this.priceManagementMessage = '';
    this.loadAdminPriceRecords(true);
  }

  resetPriceManagementFilters(): void {
    this.priceManagementSearch = '';
    this.priceManagementStatus = '';
    this.priceManagementPage = 1;
    this.priceManagementMessage = '';
    this.priceRecordsErrorMessage = '';
    this.loadAdminPriceRecords();
  }

  previousPriceManagementPage(): void {
    if (!this.priceManagementHasPreviousPage || this.isLoadingPriceRecords) {
      return;
    }

    this.priceManagementPage = Math.max(1, this.priceManagementPage - 1);
    this.loadAdminPriceRecords();
  }

  nextPriceManagementPage(): void {
    if (!this.priceManagementHasNextPage || this.isLoadingPriceRecords) {
      return;
    }

    this.priceManagementPage = Math.min(this.priceManagementPageCount, this.priceManagementPage + 1);
    this.loadAdminPriceRecords();
  }

  openPriceEditModal(record: AdminPriceRecord): void {
    this.selectedPriceRecord = record;
    this.initializePriceEditForm(record);
    this.priceEditMessage = '';
    this.priceEditErrorMessage = '';
    this.priceEditValidationErrors = [];
    this.isPriceEditModalOpen = true;
  }

  closePriceEditModal(): void {
    if (this.isSavingPriceEdit) {
      return;
    }

    this.isPriceEditModalOpen = false;
    this.selectedPriceRecord = null;
    this.priceEditMessage = '';
    this.priceEditErrorMessage = '';
    this.priceEditValidationErrors = [];
  }

  initializePriceEditForm(record: AdminPriceRecord): void {
    this.priceEditForm = {
      price: record.price,
      unit: record.unit,
      status: record.status,
      source: record.source,
    };
  }

  savePriceEdit(): void {
    if (this.isSavingPriceEdit || !this.selectedPriceRecord) {
      return;
    }

    this.priceEditValidationErrors = this.validatePriceEditForm();
    this.priceEditErrorMessage = '';

    if (this.priceEditValidationErrors.length > 0) {
      this.priceEditMessage = '';
      return;
    }

    this.isSavingPriceEdit = true;
    this.priceEditMessage = '';

    this.adminPrices.updateAdminPrice(this.selectedPriceRecord.id, {
      price: this.priceEditForm.price!,
      unit: this.priceEditForm.unit.trim(),
      status: this.priceEditForm.status,
      source: this.priceEditForm.source,
    }).subscribe({
      next: updatedRecord => {
        this.isSavingPriceEdit = false;
        this.isPriceEditModalOpen = false;
        this.selectedPriceRecord = null;
        this.priceEditValidationErrors = [];
        this.priceEditErrorMessage = '';
        this.priceManagementMessage = 'Price record updated successfully.';
        this.replaceVisiblePriceRecord(updatedRecord);
      },
      error: error => {
        this.isSavingPriceEdit = false;
        this.priceEditErrorMessage = this.getPriceEditErrorMessage(error);
      },
    });
  }

  validatePriceEditForm(): string[] {
    const errors: string[] = [];

    if (this.priceEditForm.price === null || this.priceEditForm.price === undefined) {
      errors.push('Price is required.');
    } else if (this.priceEditForm.price <= 0) {
      errors.push('Price must be greater than 0.');
    }

    if (!this.priceEditForm.unit.trim()) {
      errors.push('Unit is required.');
    }

    if (!this.priceEditForm.status) {
      errors.push('Status is required.');
    }

    if (!this.priceEditForm.source) {
      errors.push('Source is required.');
    }

    return errors;
  }

  private getPriceEditErrorMessage(error: unknown): string {
    const status = typeof error === 'object' && error !== null && 'status' in error
      ? Number((error as { status?: number }).status)
      : 0;

    if (status === 404) {
      return 'This price record could not be found. It may have been removed.';
    }

    if (status === 401 || status === 403) {
      return 'You are not authorized to update this record.';
    }

    if (status === 400) {
      return 'Some price details are invalid. Please review the form and try again.';
    }

    return 'Could not update price record. Please try again.';
  }

  private replaceVisiblePriceRecord(updatedRecord: AdminPriceRecord): void {
    const normalizedStatus = this.priceManagementStatus.trim().toLowerCase();
    const recordMatchesStatus = !normalizedStatus || updatedRecord.status.toLowerCase() === normalizedStatus;

    if (!recordMatchesStatus) {
      this.priceRecords = this.priceRecords.filter(record => record.id !== updatedRecord.id);
      this.priceManagementTotalCount = Math.max(0, this.priceManagementTotalCount - 1);
      this.priceManagementTotalPages = Math.ceil(this.priceManagementTotalCount / this.priceManagementPageSize);
      this.priceManagementHasNextPage = this.priceManagementPage < this.priceManagementPageCount;
      this.priceManagementHasPreviousPage = this.priceManagementPage > 1 && this.priceManagementTotalPages > 0;
      return;
    }

    this.priceRecords = this.priceRecords.map(record =>
      record.id === updatedRecord.id ? updatedRecord : record
    );
  }

  exportTrafficIntelligenceReport(): void {
    if (this.isExportingTrafficReport) {
      return;
    }

    this.isExportingTrafficReport = true;
    this.exportMessage = '';
    this.exportErrorMessage = '';

    this.api.getBlobResponse('/Admin/traffic-intelligence/export-pdf').subscribe({
      next: response => {
        try {
          this.downloadReport(response);
          this.exportMessage = 'Traffic report download started.';
        } catch (error) {
          this.exportErrorMessage =
            error instanceof Error ? error.message : 'Unable to export traffic report right now.';
        } finally {
          this.isExportingTrafficReport = false;
        }
      },
      error: error => {
        this.exportErrorMessage =
          error instanceof Error ? error.message : 'Unable to export traffic report right now.';
        this.isExportingTrafficReport = false;
      },
    });
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

  get pagedPeakHours(): PeakHour[] {
    const page = this.clampPeakHoursPage(this.peakHoursPage);
    const startIndex = (page - 1) * this.peakHoursPageSize;
    return this.peakHours.slice(startIndex, startIndex + this.peakHoursPageSize);
  }

  get peakHoursPageCount(): number {
    return Math.max(1, Math.ceil(this.peakHours.length / this.peakHoursPageSize));
  }

  get shouldShowPeakHoursPagination(): boolean {
    return this.peakHours.length > this.peakHoursPageSize;
  }

  goToPreviousPeakHoursPage(): void {
    this.peakHoursPage = this.clampPeakHoursPage(this.peakHoursPage - 1);
  }

  goToNextPeakHoursPage(): void {
    this.peakHoursPage = this.clampPeakHoursPage(this.peakHoursPage + 1);
  }

  goToFirstPeakHoursPage(): void {
    this.peakHoursPage = 1;
  }

  goToLastPeakHoursPage(): void {
    this.peakHoursPage = this.peakHoursPageCount;
  }

  private handleLoadError(error: unknown): void {
    this.isLoading = false;

    if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
      this.auth.logout();
      this.errorMessage = 'Please login again.';
      void this.router.navigate(['/admin']);
      return;
    }

    this.errorMessage =
      error instanceof Error ? error.message : 'Unable to load dashboard data.';
  }

  private downloadReport(response: HttpResponse<Blob>): void {
    const blob = response.body;

    if (!blob) {
      throw new Error('Downloaded report was empty.');
    }

    const fileName =
      this.getFileNameFromContentDisposition(response.headers.get('Content-Disposition')) ||
      'BazarKoto-Traffic-Intelligence-Report.pdf';
    const objectUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = objectUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(objectUrl), 0);
  }

  private getFileNameFromContentDisposition(contentDisposition: string | null): string | null {
    if (!contentDisposition) {
      return null;
    }

    const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);

    if (utf8Match?.[1]) {
      return decodeURIComponent(utf8Match[1].trim());
    }

    const fileNameMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
    return fileNameMatch?.[1]?.trim() || null;
  }

  private loadAdminPriceRecords(debounce = false): void {
    if (this.priceSearchDebounceTimer) {
      clearTimeout(this.priceSearchDebounceTimer);
      this.priceSearchDebounceTimer = null;
    }

    if (debounce) {
      this.priceSearchDebounceTimer = setTimeout(() => this.loadAdminPriceRecords(), 300);
      return;
    }

    const requestId = ++this.priceRecordsRequestId;

    this.priceRecordsRequest?.unsubscribe();
    this.isLoadingPriceRecords = true;
    this.priceRecordsErrorMessage = '';

    this.priceRecordsRequest = this.adminPrices.getAdminPrices({
      search: this.priceManagementSearch.trim(),
      status: this.priceManagementStatus,
      pageNumber: this.priceManagementPage,
      pageSize: this.priceManagementPageSize,
    }).subscribe({
      next: response => {
        if (requestId !== this.priceRecordsRequestId) {
          return;
        }

        this.priceRecords = response.data;
        this.priceManagementPage = response.pageNumber;
        this.priceManagementTotalCount = response.totalCount;
        this.priceManagementTotalPages = response.totalPages;
        this.priceManagementHasPreviousPage = response.hasPreviousPage;
        this.priceManagementHasNextPage = response.hasNextPage;
        this.isLoadingPriceRecords = false;
      },
      error: () => {
        if (requestId !== this.priceRecordsRequestId) {
          return;
        }

        this.priceRecords = [];
        this.priceManagementTotalCount = 0;
        this.priceManagementTotalPages = 0;
        this.priceManagementHasPreviousPage = false;
        this.priceManagementHasNextPage = false;
        this.priceRecordsErrorMessage = 'Could not load price records. Please try again.';
        this.isLoadingPriceRecords = false;
      },
    });
  }

  formatNumber(value: number): string {
    if (!Number.isFinite(value)) {
      return '0';
    }

    const absoluteValue = Math.abs(value);
    const sign = value < 0 ? '-' : '';

    if (absoluteValue < 1000) {
      return Math.round(value).toLocaleString();
    }

    const units = [
      { threshold: 1_000_000_000_000, divisor: 1_000_000_000_000, suffix: 't' },
      { threshold: 1_000_000_000, divisor: 1_000_000_000, suffix: 'b' },
      { threshold: 10_000_000, divisor: 10_000_000, suffix: 'crore' },
      { threshold: 100_000, divisor: 100_000, suffix: 'lakh' },
      { threshold: 1000, divisor: 1000, suffix: 'k' },
    ];
    const unit = units.find(item => absoluteValue >= item.threshold)!;
    const compactValue = absoluteValue / unit.divisor;
    const precision = compactValue >= 100 ? 0 : compactValue >= 10 ? 1 : 1;
    const formatted = compactValue
      .toFixed(precision)
      .replace(/\.0$/, '')
      .replace(/(\.\d)0$/, '$1');

    return `${sign}${formatted}${unit.suffix}`;
  }

  private clampPeakHoursPage(page: number): number {
    return Math.min(Math.max(1, page), this.peakHoursPageCount);
  }

  private formatHour(hour: number): string {
    const normalizedHour = ((hour % 24) + 24) % 24;
    const nextHour = (normalizedHour + 1) % 24;
    return `${this.formatClockHour(normalizedHour)} - ${this.formatClockHour(nextHour)}`;
  }

  private formatClockHour(hour: number): string {
    if (hour === 0) {
      return '12 AM';
    }

    if (hour === 12) {
      return '12 PM';
    }

    return hour < 12 ? `${hour} AM` : `${hour - 12} PM`;
  }
}
