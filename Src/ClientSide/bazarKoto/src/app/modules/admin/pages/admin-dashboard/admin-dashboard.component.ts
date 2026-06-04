import { CommonModule } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, HostListener, OnDestroy, OnInit, computed, signal } from '@angular/core';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  readonly peakHoursPageSize = 5;
  isLoading = signal(true);
  isLoggingOut = signal(false);
  isExportingTrafficReport = signal(false);
  isPriceManagementModalOpen = signal(false);
  errorMessage = signal('');
  exportMessage = signal('');
  exportErrorMessage = signal('');
  priceManagementMessage = signal('');
  priceManagementSearch = signal('');
  priceManagementStatus = signal('');
  priceManagementPage = signal(1);
  priceManagementTotalCount = signal(0);
  priceManagementTotalPages = signal(0);
  priceManagementHasPreviousPage = signal(false);
  priceManagementHasNextPage = signal(false);
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
  isLoadingPriceRecords = signal(false);
  isPriceEditModalOpen = signal(false);
  isSavingPriceEdit = signal(false);
  priceRecordsErrorMessage = signal('');
  priceEditMessage = signal('');
  priceEditErrorMessage = signal('');
  priceEditValidationErrors = signal<string[]>([]);
  selectedPriceRecord = signal<AdminPriceRecord | null>(null);
  priceEditForm = signal<PriceEditForm>({
    price: null,
    unit: '',
    status: '',
    source: '',
  });
  priceRecords = signal<AdminPriceRecord[]>([]);
  trafficMetrics = signal<DashboardMetric[]>([]);
  dataMetrics = signal<DashboardMetric[]>([]);
  peakHours = signal<PeakHour[]>([]);
  peakHoursPage = signal(1);
  moderationQueue = signal<AdminQueueItem[]>([]);
  pagedPeakHours = computed(() => {
    const page = this.clampPeakHoursPage(this.peakHoursPage());
    const startIndex = (page - 1) * this.peakHoursPageSize;
    return this.peakHours().slice(startIndex, startIndex + this.peakHoursPageSize);
  });
  peakHoursPageCount = computed(() => Math.max(1, Math.ceil(this.peakHours().length / this.peakHoursPageSize)));
  shouldShowPeakHoursPagination = computed(() => this.peakHours().length > this.peakHoursPageSize);
  priceManagementPageCount = computed(() => Math.max(1, this.priceManagementTotalPages()));
  priceManagementPageStart = computed(() => this.priceManagementTotalCount() === 0
    ? 0
    : (this.priceManagementPage() - 1) * this.priceManagementPageSize + 1);
  priceManagementPageEnd = computed(() =>
    Math.min(this.priceManagementPage() * this.priceManagementPageSize, this.priceManagementTotalCount())
  );
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
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.api.get<AdminDashboardResponse>('/Admin/dashboard').subscribe({
      next: dashboard => {
        this.bindDashboard(dashboard);
        this.isLoading.set(false);
      },
      error: error => {
        this.handleLoadError(error);
      },
    });
  }

  private bindDashboard(dashboard: AdminDashboardResponse): void {
    const peakHour = dashboard.peakHours[0];
    this.peakHoursPage.set(1);

    this.trafficMetrics.set([
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
    ]);

    this.dataMetrics.set([
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
    ]);

    this.peakHours.set(dashboard.peakHours.map(hour => ({
      time: this.formatHour(hour.hour),
      visits: this.formatNumber(hour.visitCount),
      share: 'Backend traffic data',
    })));

    this.moderationQueue.set([
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
    ]);
  }

  openQueueItem(item: AdminQueueItem): void {
    if (item.route) {
      void this.router.navigate([item.route]);
    }
  }

  openPriceManagementModal(): void {
    this.isPriceManagementModalOpen.set(true);
    this.priceManagementMessage.set('');
    this.priceManagementPage.set(1);
    this.loadAdminPriceRecords();
  }

  closePriceManagementModal(): void {
    if (this.isSavingPriceEdit()) {
      return;
    }

    this.priceRecordsRequest?.unsubscribe();
    this.closePriceEditModal();
    this.isPriceManagementModalOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.isSavingPriceEdit()) {
      return;
    }

    if (this.isPriceEditModalOpen()) {
      this.closePriceEditModal();
      return;
    }

    if (this.isPriceManagementModalOpen()) {
      this.closePriceManagementModal();
    }
  }

  onPriceManagementFiltersChange(): void {
    this.priceManagementPage.set(1);
    this.priceManagementMessage.set('');
    this.loadAdminPriceRecords(true);
  }

  resetPriceManagementFilters(): void {
    this.priceManagementSearch.set('');
    this.priceManagementStatus.set('');
    this.priceManagementPage.set(1);
    this.priceManagementMessage.set('');
    this.priceRecordsErrorMessage.set('');
    this.loadAdminPriceRecords();
  }

  previousPriceManagementPage(): void {
    if (!this.priceManagementHasPreviousPage() || this.isLoadingPriceRecords()) {
      return;
    }

    this.priceManagementPage.update(page => Math.max(1, page - 1));
    this.loadAdminPriceRecords();
  }

  nextPriceManagementPage(): void {
    if (!this.priceManagementHasNextPage() || this.isLoadingPriceRecords()) {
      return;
    }

    this.priceManagementPage.update(page => Math.min(this.priceManagementPageCount(), page + 1));
    this.loadAdminPriceRecords();
  }

  openPriceEditModal(record: AdminPriceRecord): void {
    this.selectedPriceRecord.set(record);
    this.initializePriceEditForm(record);
    this.priceEditMessage.set('');
    this.priceEditErrorMessage.set('');
    this.priceEditValidationErrors.set([]);
    this.isPriceEditModalOpen.set(true);
  }

  closePriceEditModal(): void {
    if (this.isSavingPriceEdit()) {
      return;
    }

    this.isPriceEditModalOpen.set(false);
    this.selectedPriceRecord.set(null);
    this.priceEditMessage.set('');
    this.priceEditErrorMessage.set('');
    this.priceEditValidationErrors.set([]);
  }

  initializePriceEditForm(record: AdminPriceRecord): void {
    this.priceEditForm.set({
      price: record.price,
      unit: record.unit,
      status: record.status,
      source: record.source,
    });
  }

  updatePriceEditForm<K extends keyof PriceEditForm>(field: K, value: PriceEditForm[K]): void {
    this.priceEditForm.update(form => ({
      ...form,
      [field]: value,
    }));
  }

  savePriceEdit(): void {
    const selectedPriceRecord = this.selectedPriceRecord();

    if (this.isSavingPriceEdit() || !selectedPriceRecord) {
      return;
    }

    this.priceEditValidationErrors.set(this.validatePriceEditForm());
    this.priceEditErrorMessage.set('');

    if (this.priceEditValidationErrors().length > 0) {
      this.priceEditMessage.set('');
      return;
    }

    this.isSavingPriceEdit.set(true);
    this.priceEditMessage.set('');
    const priceEditForm = this.priceEditForm();

    this.adminPrices.updateAdminPrice(selectedPriceRecord.id, {
      price: priceEditForm.price!,
      unit: priceEditForm.unit.trim(),
      status: priceEditForm.status,
      source: priceEditForm.source,
    }).subscribe({
      next: updatedRecord => {
        this.isSavingPriceEdit.set(false);
        this.isPriceEditModalOpen.set(false);
        this.selectedPriceRecord.set(null);
        this.priceEditValidationErrors.set([]);
        this.priceEditErrorMessage.set('');
        this.priceManagementMessage.set('Price record updated successfully.');
        this.replaceVisiblePriceRecord(updatedRecord);
      },
      error: error => {
        this.isSavingPriceEdit.set(false);
        this.priceEditErrorMessage.set(this.getPriceEditErrorMessage(error));
      },
    });
  }

  validatePriceEditForm(): string[] {
    const errors: string[] = [];
    const priceEditForm = this.priceEditForm();

    if (priceEditForm.price === null || priceEditForm.price === undefined) {
      errors.push('Price is required.');
    } else if (priceEditForm.price <= 0) {
      errors.push('Price must be greater than 0.');
    }

    if (!priceEditForm.unit.trim()) {
      errors.push('Unit is required.');
    }

    if (!priceEditForm.status) {
      errors.push('Status is required.');
    }

    if (!priceEditForm.source) {
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
    const normalizedStatus = this.priceManagementStatus().trim().toLowerCase();
    const recordMatchesStatus = !normalizedStatus || updatedRecord.status.toLowerCase() === normalizedStatus;

    if (!recordMatchesStatus) {
      this.priceRecords.update(records => records.filter(record => record.id !== updatedRecord.id));
      this.priceManagementTotalCount.update(totalCount => Math.max(0, totalCount - 1));
      this.priceManagementTotalPages.set(Math.ceil(this.priceManagementTotalCount() / this.priceManagementPageSize));
      this.priceManagementHasNextPage.set(this.priceManagementPage() < this.priceManagementPageCount());
      this.priceManagementHasPreviousPage.set(this.priceManagementPage() > 1 && this.priceManagementTotalPages() > 0);
      return;
    }

    this.priceRecords.update(records => records.map(record =>
      record.id === updatedRecord.id ? updatedRecord : record
    ));
  }

  exportTrafficIntelligenceReport(): void {
    if (this.isExportingTrafficReport()) {
      return;
    }

    this.isExportingTrafficReport.set(true);
    this.exportMessage.set('');
    this.exportErrorMessage.set('');

    this.api.getBlobResponse('/Admin/traffic-intelligence/export-pdf').subscribe({
      next: response => {
        try {
          this.downloadReport(response);
          this.exportMessage.set('Traffic report download started.');
        } catch (error) {
          this.exportErrorMessage.set(
            error instanceof Error ? error.message : 'Unable to export traffic report right now.'
          );
        } finally {
          this.isExportingTrafficReport.set(false);
        }
      },
      error: error => {
        this.exportErrorMessage.set(
          error instanceof Error ? error.message : 'Unable to export traffic report right now.'
        );
        this.isExportingTrafficReport.set(false);
      },
    });
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

  goToPreviousPeakHoursPage(): void {
    this.peakHoursPage.update(page => this.clampPeakHoursPage(page - 1));
  }

  goToNextPeakHoursPage(): void {
    this.peakHoursPage.update(page => this.clampPeakHoursPage(page + 1));
  }

  goToFirstPeakHoursPage(): void {
    this.peakHoursPage.set(1);
  }

  goToLastPeakHoursPage(): void {
    this.peakHoursPage.set(this.peakHoursPageCount());
  }

  private handleLoadError(error: unknown): void {
    this.isLoading.set(false);

    if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
      this.auth.logout();
      this.errorMessage.set('Please login again.');
      void this.router.navigate(['/admin']);
      return;
    }

    this.errorMessage.set(error instanceof Error ? error.message : 'Unable to load dashboard data.');
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
    this.isLoadingPriceRecords.set(true);
    this.priceRecordsErrorMessage.set('');

    this.priceRecordsRequest = this.adminPrices.getAdminPrices({
      search: this.priceManagementSearch().trim(),
      status: this.priceManagementStatus(),
      pageNumber: this.priceManagementPage(),
      pageSize: this.priceManagementPageSize,
    }).subscribe({
      next: response => {
        if (requestId !== this.priceRecordsRequestId) {
          return;
        }

        this.priceRecords.set(response.data);
        this.priceManagementPage.set(response.pageNumber);
        this.priceManagementTotalCount.set(response.totalCount);
        this.priceManagementTotalPages.set(response.totalPages);
        this.priceManagementHasPreviousPage.set(response.hasPreviousPage);
        this.priceManagementHasNextPage.set(response.hasNextPage);
        this.isLoadingPriceRecords.set(false);
      },
      error: () => {
        if (requestId !== this.priceRecordsRequestId) {
          return;
        }

        this.priceRecords.set([]);
        this.priceManagementTotalCount.set(0);
        this.priceManagementTotalPages.set(0);
        this.priceManagementHasPreviousPage.set(false);
        this.priceManagementHasNextPage.set(false);
        this.priceRecordsErrorMessage.set('Could not load price records. Please try again.');
        this.isLoadingPriceRecords.set(false);
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
    return Math.min(Math.max(1, page), this.peakHoursPageCount());
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
