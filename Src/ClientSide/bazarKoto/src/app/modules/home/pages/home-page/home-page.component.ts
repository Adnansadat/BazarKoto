import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Api } from '../../../../core/services/api';

type PriceCategory = 'all' | 'vegetables' | 'staples' | 'protein';

interface AveragePrice {
  name: string;
  market: string;
  category: Exclude<PriceCategory, 'all'>;
  unit: string;
  average: number;
  change: number;
  searchable: string;
}

interface PriceSubmissionResponse {
  marketName: string;
  productName: string;
  category: string;
  unit: string;
  pricePerUnit: number;
  priceDate: string;
  createdAt: string;
}

interface CategoryLink {
  titleKey: string;
  descriptionKey: string;
  route: string;
}

interface BenefitItem {
  titleKey: string;
  descriptionKey: string;
}

interface FaqItem {
  questionKey: string;
  answerKey: string;
}

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.scss']
})
export class HomePageComponent implements OnInit {
  searchTerm = '';
  selectedCategory: PriceCategory = 'all';
  isLoadingPrices = true;
  priceErrorMessage = '';
  averagePrices: AveragePrice[] = [];

  constructor(
    private translate: TranslateService,
    private readonly api: Api,
  ) {}

  ngOnInit(): void {
    this.loadPrices();
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.translate.defaultLang || 'en';
  }

  readonly categories: Array<{ labelKey: string; value: PriceCategory }> = [
    { labelKey: 'home.category.all', value: 'all' },
    { labelKey: 'home.category.vegetables', value: 'vegetables' },
    { labelKey: 'home.category.staples', value: 'staples' },
    { labelKey: 'home.category.protein', value: 'protein' }
  ];

  readonly miniCategories: Array<{ labelKey: string; value: Exclude<PriceCategory, 'all'> }> = [
    { labelKey: 'home.category.vegetables', value: 'vegetables' },
    { labelKey: 'home.category.staples', value: 'staples' },
    { labelKey: 'home.category.protein', value: 'protein' }
  ];

  readonly categoryLinks: CategoryLink[] = [
    {
      titleKey: 'home.categoryLink.vegetables.title',
      descriptionKey: 'home.categoryLink.vegetables.description',
      route: '/products'
    },
    {
      titleKey: 'home.categoryLink.staples.title',
      descriptionKey: 'home.categoryLink.staples.description',
      route: '/products'
    },
    {
      titleKey: 'home.categoryLink.protein.title',
      descriptionKey: 'home.categoryLink.protein.description',
      route: '/products'
    },
    {
      titleKey: 'home.categoryLink.markets.title',
      descriptionKey: 'home.categoryLink.markets.description',
      route: '/markets'
    }
  ];

  readonly benefits: BenefitItem[] = [
    {
      titleKey: 'home.benefit.compare.title',
      descriptionKey: 'home.benefit.compare.description'
    },
    {
      titleKey: 'home.benefit.track.title',
      descriptionKey: 'home.benefit.track.description'
    },
    {
      titleKey: 'home.benefit.contribute.title',
      descriptionKey: 'home.benefit.contribute.description'
    }
  ];

  readonly faqs: FaqItem[] = [
    {
      questionKey: 'home.faq.q1.question',
      answerKey: 'home.faq.q1.answer'
    },
    {
      questionKey: 'home.faq.q2.question',
      answerKey: 'home.faq.q2.answer'
    },
    {
      questionKey: 'home.faq.q3.question',
      answerKey: 'home.faq.q3.answer'
    },
    {
      questionKey: 'home.faq.q4.question',
      answerKey: 'home.faq.q4.answer'
    },
    {
      questionKey: 'home.faq.q5.question',
      answerKey: 'home.faq.q5.answer'
    },
    {
      questionKey: 'home.faq.q6.question',
      answerKey: 'home.faq.q6.answer'
    }
  ];

  get filteredPrices(): AveragePrice[] {
    const normalizedSearch = this.searchTerm.trim().toLowerCase();

    return this.averagePrices.filter((price) => {
      const matchesCategory =
        this.selectedCategory === 'all' || price.category === this.selectedCategory;

      const matchesSearch =
        !normalizedSearch ||
        price.searchable.toLowerCase().includes(normalizedSearch) ||
        price.name.toLowerCase().includes(normalizedSearch) ||
        price.market.toLowerCase().includes(normalizedSearch);

      return matchesCategory && matchesSearch;
    });
  }

  getProductInitial(name: string): string {
    return name ? name.charAt(0) : '';
  }

  private loadPrices(): void {
    this.isLoadingPrices = true;
    this.priceErrorMessage = '';

    this.api.get<PriceSubmissionResponse[]>('/Prices').subscribe({
      next: prices => {
        this.averagePrices = this.toAveragePrices(prices);
        this.isLoadingPrices = false;
      },
      error: error => {
        this.priceErrorMessage = error instanceof Error ? error.message : 'Unable to load prices.';
        this.isLoadingPrices = false;
      },
    });
  }

  private toAveragePrices(prices: PriceSubmissionResponse[]): AveragePrice[] {
    const groupedPrices = new Map<string, PriceSubmissionResponse[]>();

    for (const price of prices) {
      const key = `${price.productName}|${price.marketName}|${price.unit}`;
      groupedPrices.set(key, [...(groupedPrices.get(key) ?? []), price]);
    }

    return Array.from(groupedPrices.values()).map(group => {
      const ordered = [...group].sort((first, second) => this.getPriceTime(second) - this.getPriceTime(first));
      const latest = ordered[0];
      const previous = ordered[1];

      return {
        name: latest.productName,
        market: latest.marketName,
        category: this.mapCategory(latest.category),
        unit: latest.unit,
        average: latest.pricePerUnit,
        change: this.calculateChange(latest, previous),
        searchable: `${latest.productName} ${latest.marketName} ${latest.category}`,
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
