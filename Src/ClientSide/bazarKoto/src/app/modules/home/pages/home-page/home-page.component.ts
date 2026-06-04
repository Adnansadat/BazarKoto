import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Api } from '../../../../core/services/api';

type PriceCategory = 'all' | 'vegetables' | 'staples' | 'protein';

interface AveragePrice {
  productNameEn: string;
  productNameBn?: string;
  market: string;
  category: Exclude<PriceCategory, 'all'>;
  categoryNameEn?: string;
  categoryNameBn?: string;
  unit: string;
  average: number;
  change: number;
  searchable: string;
}

interface PriceSubmissionResponse {
  marketName?: string;
  productName?: string;
  productNameEn?: string;
  productNameBn?: string;
  category?: string;
  categoryNameEn?: string;
  categoryNameBn?: string;
  unit: string;
  pricePerUnit: number;
  priceDate: string;
  createdAt?: string;
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
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePageComponent implements OnInit {
  readonly selectedCategory = signal<PriceCategory>('all');
  readonly isLoadingPrices = signal(true);
  readonly priceErrorMessageKey = signal('');
  readonly averagePrices = signal<AveragePrice[]>([]);

  readonly filteredPrices = computed(() => {
    const selectedCategory = this.selectedCategory();

    return this.averagePrices().filter((price) => {
      return selectedCategory === 'all' || price.category === selectedCategory;
    });
  });

  readonly productPrices = computed(() => this.filteredPrices().slice(0, 10));

  readonly phoneFilteredPrices = computed(() => {
    const selectedCategory = this.selectedCategory();

    return this.phonePrices.filter((price) => {
      return selectedCategory === 'all' || price.category === selectedCategory;
    });
  });

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

  selectCategory(category: PriceCategory): void {
    this.selectedCategory.set(category);
  }

  readonly categories: Array<{ labelKey: string; value: PriceCategory }> = [
    { labelKey: 'home.category.all', value: 'all' },
    { labelKey: 'home.category.vegetables', value: 'vegetables' },
    { labelKey: 'home.category.staples', value: 'staples' },
    { labelKey: 'home.category.protein', value: 'protein' }
  ];

  readonly miniCategories: Array<{ labelKey: string; value: PriceCategory }> = [
    { labelKey: 'home.category.all', value: 'all' },
    { labelKey: 'home.category.vegetables', value: 'vegetables' },
    { labelKey: 'home.category.staples', value: 'staples' },
    { labelKey: 'home.category.protein', value: 'protein' }
  ];

  readonly phonePrices: AveragePrice[] = [
    {
      productNameEn: 'Onion',
      productNameBn: 'পেঁয়াজ',
      market: 'Dhaka, Bangladesh',
      category: 'vegetables',
      categoryNameEn: 'Vegetables',
      categoryNameBn: 'সবজি',
      unit: 'kg',
      average: 60,
      change: 0,
      searchable: 'Onion Dhaka Bangladesh vegetables',
    },
    {
      productNameEn: 'Potato',
      productNameBn: 'আলু',
      market: 'Dhaka, Bangladesh',
      category: 'vegetables',
      categoryNameEn: 'Vegetables',
      categoryNameBn: 'সবজি',
      unit: 'kg',
      average: 45,
      change: 0,
      searchable: 'Potato Dhaka Bangladesh vegetables',
    },
    {
      productNameEn: 'Rice Miniket',
      productNameBn: 'মিনিকেট চাল',
      market: 'Dhaka, Bangladesh',
      category: 'staples',
      categoryNameEn: 'Rice & Staples',
      categoryNameBn: 'চাল ও প্রধান খাদ্য',
      unit: 'kg',
      average: 85,
      change: 0,
      searchable: 'Rice Miniket Dhaka Bangladesh staples',
    },
    {
      productNameEn: 'Soybean Oil',
      productNameBn: 'সয়াবিন তেল',
      market: 'Dhaka, Bangladesh',
      category: 'staples',
      categoryNameEn: 'Staples',
      categoryNameBn: 'প্রধান খাদ্য',
      unit: 'litre',
      average: 195,
      change: 0,
      searchable: 'Soybean Oil Dhaka Bangladesh staples',
    },
    {
      productNameEn: 'Egg',
      productNameBn: 'ডিম',
      market: 'Dhaka, Bangladesh',
      category: 'protein',
      categoryNameEn: 'Protein',
      categoryNameBn: 'প্রোটিন',
      unit: 'dozen',
      average: 150,
      change: 0,
      searchable: 'Egg Dhaka Bangladesh protein',
    },
    {
      productNameEn: 'Rui Fish',
      productNameBn: 'রুই মাছ',
      market: 'Dhaka, Bangladesh',
      category: 'protein',
      categoryNameEn: 'Protein',
      categoryNameBn: 'প্রোটিন',
      unit: 'kg',
      average: 360,
      change: 0,
      searchable: 'Rui Fish Dhaka Bangladesh protein',
    },
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

  getProductInitial(price: AveragePrice): string {
    const name = this.getLocalizedProductName(price);
    return name ? name.charAt(0) : '';
  }

  getLocalizedProductName(price: AveragePrice): string {
    if (this.currentLanguage === 'bn' && price.productNameBn) {
      return price.productNameBn;
    }

    return price.productNameEn || price.productNameBn || '';
  }

  getLocalizedUnitKey(price: AveragePrice): string {
    const normalizedUnit = price.unit.toLowerCase().trim();
    const unitKeyMap: Record<string, string> = {
      kg: 'home.unit.kg',
      kilogram: 'home.unit.kg',
      kilograms: 'home.unit.kg',
      dozen: 'home.unit.dozen',
      litre: 'home.unit.liter',
      liter: 'home.unit.liter',
      l: 'home.unit.liter',
    };

    return unitKeyMap[normalizedUnit] ?? '';
  }

  private loadPrices(): void {
    this.isLoadingPrices.set(true);
    this.priceErrorMessageKey.set('');

    this.api.get<PriceSubmissionResponse[]>('/Prices', { pageSize: 100 }).subscribe({
      next: prices => {
        this.averagePrices.set(this.toAveragePrices(prices));
        this.isLoadingPrices.set(false);
      },
      error: () => {
        this.priceErrorMessageKey.set('home.prices.error');
        this.isLoadingPrices.set(false);
      },
    });
  }

  private toAveragePrices(prices: PriceSubmissionResponse[]): AveragePrice[] {
    const groupedPrices = new Map<string, PriceSubmissionResponse[]>();

    for (const price of prices) {
      const productName = this.getStableProductName(price);

      if (!productName || price.pricePerUnit <= 0) {
        continue;
      }

      const key = `${productName}|${price.marketName ?? ''}|${price.unit}`;
      groupedPrices.set(key, [...(groupedPrices.get(key) ?? []), price]);
    }

    return Array.from(groupedPrices.values()).map(group => {
      const ordered = [...group].sort((first, second) => this.getPriceTime(second) - this.getPriceTime(first));
      const latest = ordered[0];
      const previous = ordered[1];
      const productName = this.getStableProductName(latest);
      const categoryName = latest.categoryNameEn || latest.category || latest.categoryNameBn || '';

      return {
        productNameEn: latest.productNameEn || latest.productName || latest.productNameBn || '',
        productNameBn: latest.productNameBn,
        market: latest.marketName ?? 'Bangladesh',
        category: this.mapCategory(categoryName),
        categoryNameEn: latest.categoryNameEn || latest.category,
        categoryNameBn: latest.categoryNameBn,
        unit: latest.unit,
        average: latest.pricePerUnit,
        change: this.calculateChange(latest, previous),
        searchable: `${productName} ${latest.marketName ?? ''} ${categoryName} ${latest.categoryNameBn ?? ''}`,
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

  private getStableProductName(price: PriceSubmissionResponse): string {
    return price.productNameEn || price.productName || price.productNameBn || '';
  }

  private mapCategory(category: string): Exclude<PriceCategory, 'all'> {
    const normalized = category.toLowerCase();

    if (
      normalized.includes('rice') ||
      normalized.includes('grain') ||
      normalized.includes('staple') ||
      normalized.includes('grocery') ||
      normalized.includes('flour') ||
      normalized.includes('oil')
    ) {
      return 'staples';
    }

    if (
      normalized.includes('fish') ||
      normalized.includes('meat') ||
      normalized.includes('poultry') ||
      normalized.includes('egg') ||
      normalized.includes('protein')
    ) {
      return 'protein';
    }

    return 'vegetables';
  }
}
