import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../../core/services/api';

type PriceCategory = 'all' | 'vegetables' | 'staples' | 'protein';

interface PriceSubmissionResponse {
  productName: string;
  marketName: string;
  category: string;
  pricePerUnit: number;
  unit: string;
  priceDate: string;
  createdAt: string;
}

interface PriceTableRow {
  productName: string;
  marketName: string;
  category: Exclude<PriceCategory, 'all'>;
  pricePerUnit: number;
  unit: string;
  change: number;
}

@Component({
  selector: 'app-live-prices-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './live-prices-page.component.html',
  styleUrl: './live-prices-page.component.scss',
})
export class LivePricesPageComponent implements OnInit {
  searchTerm = '';
  selectedCategory: PriceCategory = 'all';
  currentPage = 1;
  readonly pageSize = 20;
  isLoading = true;
  errorMessage = '';
  priceRows: PriceTableRow[] = [];

  readonly categories: Array<{ label: string; value: PriceCategory }> = [
    { label: 'All', value: 'all' },
    { label: 'Vegetables', value: 'vegetables' },
    { label: 'Staples', value: 'staples' },
    { label: 'Protein', value: 'protein' },
  ];

  constructor(private readonly api: Api) {}

  ngOnInit(): void {
    this.loadPrices();
  }

  get filteredRows(): PriceTableRow[] {
    const search = this.searchTerm.trim().toLowerCase();

    return this.priceRows.filter(row => {
      const matchesCategory = this.selectedCategory === 'all' || row.category === this.selectedCategory;
      const matchesSearch =
        !search ||
        row.productName.toLowerCase().includes(search) ||
        row.marketName.toLowerCase().includes(search) ||
        row.unit.toLowerCase().includes(search);

      return matchesCategory && matchesSearch;
    });
  }

  get pagedRows(): PriceTableRow[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredRows.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredRows.length / this.pageSize));
  }

  get pageStart(): number {
    return this.filteredRows.length === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.filteredRows.length);
  }

  setCategory(category: PriceCategory): void {
    this.selectedCategory = category;
    this.currentPage = 1;
  }

  onSearchChange(): void {
    this.currentPage = 1;
  }

  previousPage(): void {
    this.currentPage = Math.max(1, this.currentPage - 1);
  }

  nextPage(): void {
    this.currentPage = Math.min(this.totalPages, this.currentPage + 1);
  }

  private loadPrices(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.api.get<PriceSubmissionResponse[]>('/Prices', { pageSize: 100 }).subscribe({
      next: prices => {
        this.priceRows = this.toPriceRows(prices);
        this.isLoading = false;
      },
      error: error => {
        this.errorMessage = error instanceof Error ? error.message : 'Unable to load live prices.';
        this.isLoading = false;
      },
    });
  }

  private toPriceRows(prices: PriceSubmissionResponse[]): PriceTableRow[] {
    const groups = new Map<string, PriceSubmissionResponse[]>();

    for (const price of prices) {
      const key = `${price.productName}|${price.marketName}|${price.unit}`;
      groups.set(key, [...(groups.get(key) ?? []), price]);
    }

    return Array.from(groups.values()).map(group => {
      const ordered = [...group].sort((first, second) => this.getPriceTime(second) - this.getPriceTime(first));
      const latest = ordered[0];
      const previous = ordered[1];

      return {
        productName: latest.productName,
        marketName: latest.marketName,
        category: this.mapCategory(latest.category),
        pricePerUnit: latest.pricePerUnit,
        unit: latest.unit,
        change: this.calculateChange(latest, previous),
      };
    });
  }

  private getPriceTime(price: PriceSubmissionResponse): number {
    return new Date(price.createdAt || price.priceDate).getTime();
  }

  private calculateChange(latest: PriceSubmissionResponse, previous?: PriceSubmissionResponse): number {
    if (!previous || previous.pricePerUnit === 0) {
      return 0;
    }

    return Math.round(((latest.pricePerUnit - previous.pricePerUnit) / previous.pricePerUnit) * 100);
  }

  private mapCategory(category: string): Exclude<PriceCategory, 'all'> {
    const normalized = category.toLowerCase();

    if (normalized.includes('rice') || normalized.includes('staple') || normalized.includes('grocery')) {
      return 'staples';
    }

    if (normalized.includes('fish') || normalized.includes('meat') || normalized.includes('protein')) {
      return 'protein';
    }

    return 'vegetables';
  }
}
