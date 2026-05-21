import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

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
}

interface PeakHour {
  time: string;
  visits: string;
  share: string;
}

interface ManagementArea {
  title: string;
  description: string;
  actions: string[];
}

interface AdminDashboardResponse {
  traffic: {
    totalVisits: number;
    uniqueVisitors: number;
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
  imports: [CommonModule],
  standalone: true,
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
})
export class AdminDashboardComponent implements OnInit {
  isLoading = true;
  errorMessage = '';
  trafficMetrics: DashboardMetric[] = [];
  dataMetrics: DashboardMetric[] = [];
  peakHours: PeakHour[] = [];
  moderationQueue: AdminQueueItem[] = [];

  readonly managementAreas: ManagementArea[] = [
    {
      title: 'Market data',
      description: 'Approve, edit, merge, or archive market locations and contributor activity.',
      actions: ['Review markets', 'Merge duplicates', 'Audit locations'],
    },
    {
      title: 'Product data',
      description: 'Manage categories, product names, default units, and product states.',
      actions: ['Edit products', 'Resolve duplicates', 'Manage categories'],
    },
    {
      title: 'Price data',
      description: 'Validate submitted prices, inspect sources, and flag unusual bazar rates.',
      actions: ['Review prices', 'Flag outliers', 'Export records'],
    },
  ];

  constructor(
    private readonly api: Api,
    private readonly auth: Auth,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
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
        trend: `${this.formatNumber(dashboard.traffic.uniqueVisitors)} unique visitors`,
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
        label: 'Pending market approvals',
        count: dashboard.moderation.pendingMarkets,
        helper: 'New or edited market records',
      },
      {
        label: 'Flagged price submissions',
        count: dashboard.moderation.flaggedPriceSubmissions,
        helper: 'Outlier prices needing validation',
      },
      {
        label: 'Pending product reviews',
        count: dashboard.moderation.pendingProducts,
        helper: 'Product records waiting for review',
      },
      {
        label: 'User support messages',
        count: dashboard.moderation.pendingContactMessages,
        helper: 'Contact and correction requests',
      },
    ];
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

  private formatNumber(value: number): string {
    return value.toLocaleString();
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
