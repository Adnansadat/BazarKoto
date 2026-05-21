import { CommonModule, DOCUMENT } from '@angular/common';
import { AfterViewInit, Component, DoCheck, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { finalize, Subscription } from 'rxjs';
import { Api } from '../../../../core/services/api';
import { DraftService } from '../../../../core/services/draft';

interface SelectOption {
  value: string;
  labelKey: string;
}

interface LocationResponse {
  id: string;
  nameEn: string;
  nameBn: string;
  slug: string;
}

interface MarketResponse {
  id: string;
  marketName: string;
  area: string;
  divisionId: string;
  districtId: string;
  upazilaId: string;
  unionOrWardId?: string | null;
}

interface ProductCategoryResponse {
  id: string;
  nameEn: string;
  nameBn: string;
}

interface ProductResponse {
  id: string;
  categoryId: string;
  categoryNameEn: string;
  categoryNameBn: string;
  nameEn: string;
  nameBn: string;
  primaryUnit: string;
  productState: string;
}

interface PriceSubmissionResponse {
  id: string;
  productNameEn: string;
  productNameBn: string;
  marketName: string;
  pricePerUnit: number;
  unit: string;
}

interface PriceSummaryResponse {
  productId: string;
  productName: string;
  unit: string;
  minimumPrice: number;
  maximumPrice: number;
  averagePrice: number;
  submissionCount: number;
}

@Component({
  selector: 'app-prices-page',
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
  standalone: true,
  templateUrl: './prices-page.component.html',
  styleUrl: './prices-page.component.scss',
})
export class PricesPageComponent implements AfterViewInit, OnInit, OnDestroy, DoCheck {
  @ViewChild('marketInput') private marketInput?: ElementRef<HTMLInputElement>;

  private readonly siteUrl = 'https://www.bazarkoto.com';
  private readonly marketDraftKey = 'bazarkoto.market.draft';
  private readonly productDraftKey = 'bazarkoto.product.draft';
  private readonly priceDraftKey = 'bazarkoto.price.draft';
  private readonly pageUrl = `${this.siteUrl}/prices`;
  private readonly ogImageUrl = `${this.siteUrl}/images/bazar-hero.png`;
  private readonly jsonLdScriptId = 'prices-page-json-ld';
  private lastDraftJson = '';
  private langChangeSubscription?: Subscription;

  selectedDivisionId = '';
  selectedDistrictId = '';
  selectedUpazilaId = '';
  selectedUnionOrWardId = '';
  selectedMarketId = '';
  selectedProductId = '';
  selectedCategoryId = '';
  selectedUnit = 'kg';
  price = 0;
  quantity = 1;
  priceDate = new Date().toISOString().slice(0, 10);
  priceTime = '09:30';
  sellerType = 'Retail';
  priceSource = 'Observed in market';
  quality = 'Standard';
  notes = '';
  isThankYouOpen = false;
  isLoadingPrices = true;
  isLoadingDivisions = false;
  isLoadingDistricts = false;
  isLoadingUpazilas = false;
  isLoadingUnionOrWards = false;
  isLoadingMarkets = false;
  isLoadingCategories = false;
  isLoadingProducts = false;
  isLoadingSummary = false;
  isSubmittingPrice = false;
  priceErrorMessage = '';
  priceSuccessMessage = '';
  locationErrorMessage = '';
  productErrorMessage = '';
  marketSearch = '';
  productSearch = '';
  divisions: LocationResponse[] = [];
  districts: LocationResponse[] = [];
  upazilas: LocationResponse[] = [];
  unionOrWards: LocationResponse[] = [];
  categories: ProductCategoryResponse[] = [];
  markets: MarketResponse[] = [];
  products: ProductResponse[] = [];
  todayPrices: PriceSubmissionResponse[] = [];
  priceSummary?: PriceSummaryResponse;

  constructor(
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly translate: TranslateService,
    private readonly api: Api,
    private readonly drafts: DraftService,
    @Inject(DOCUMENT) private readonly document: Document,
  ) {}

  readonly units: SelectOption[] = [
    { value: 'kg', labelKey: 'prices.unit.kg' },
    { value: 'gram', labelKey: 'prices.unit.gram' },
    { value: 'piece', labelKey: 'prices.unit.piece' },
    { value: 'dozen', labelKey: 'prices.unit.dozen' },
    { value: 'litre', labelKey: 'prices.unit.litre' },
    { value: 'packet', labelKey: 'prices.unit.packet' },
  ];

  readonly sellerTypes: SelectOption[] = [
    { value: 'Retail', labelKey: 'prices.sellerType.retail' },
    { value: 'Wholesale', labelKey: 'prices.sellerType.wholesale' },
    { value: 'Street vendor', labelKey: 'prices.sellerType.streetVendor' },
  ];

  readonly sources: SelectOption[] = [
    { value: 'Observed in market', labelKey: 'prices.source.observed' },
    { value: 'Purchased personally', labelKey: 'prices.source.purchased' },
    { value: 'Seller quoted', labelKey: 'prices.source.quoted' },
  ];

  readonly qualities: SelectOption[] = [
    { value: 'Standard', labelKey: 'prices.quality.standard' },
    { value: 'Premium', labelKey: 'prices.quality.premium' },
    { value: 'Low grade', labelKey: 'prices.quality.lowGrade' },
  ];

  ngOnInit(): void {
    this.updateSeo();
    this.restoreDraft();
    this.loadPageData();
    this.langChangeSubscription = this.translate.onLangChange.subscribe(() => this.updateSeo());
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.marketInput?.nativeElement.focus());
  }

  ngOnDestroy(): void {
    this.langChangeSubscription?.unsubscribe();
    this.document.getElementById(this.jsonLdScriptId)?.remove();
  }

  ngDoCheck(): void {
    this.persistDraftIfChanged();
  }

  get totalPrice(): number {
    return this.price * this.quantity;
  }

  get selectedUnitLabelKey(): string {
    return this.units.find((unit) => unit.value === this.selectedUnit)?.labelKey ?? '';
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.translate.defaultLang || 'en';
  }

  get selectedMarketName(): string {
    return this.markets.find(market => market.id === this.selectedMarketId)?.marketName ?? '';
  }

  get selectedProductName(): string {
    const product = this.products.find(item => item.id === this.selectedProductId);
    return product ? this.getProductName(product) : '';
  }

  get selectedCategoryName(): string {
    const category = this.categories.find(item => item.id === this.selectedCategoryId);
    return category ? this.getCategoryName(category) : '';
  }

  focusSubmissionForm(): void {
    this.marketInput?.nativeElement.focus();
    this.marketInput?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  onDivisionChange(): void {
    this.selectedDistrictId = '';
    this.selectedUpazilaId = '';
    this.selectedUnionOrWardId = '';
    this.selectedMarketId = '';
    this.districts = [];
    this.upazilas = [];
    this.unionOrWards = [];
    this.markets = [];
    this.loadDistricts();
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onDistrictChange(): void {
    this.selectedUpazilaId = '';
    this.selectedUnionOrWardId = '';
    this.selectedMarketId = '';
    this.upazilas = [];
    this.unionOrWards = [];
    this.markets = [];
    this.loadUpazilas();
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onUpazilaChange(): void {
    this.selectedUnionOrWardId = '';
    this.selectedMarketId = '';
    this.unionOrWards = [];
    this.markets = [];
    this.loadUnionOrWards();
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onUnionOrWardChange(): void {
    this.selectedMarketId = '';
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onMarketChange(): void {
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onCategoryChange(): void {
    this.selectedProductId = '';
    this.products = [];
    this.loadProducts();
    this.loadTodayPrices();
  }

  onProductChange(): void {
    const product = this.products.find(item => item.id === this.selectedProductId);
    this.selectedUnit = product?.primaryUnit ?? this.selectedUnit;
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  submitPrice(): void {
    this.priceErrorMessage = '';
    this.priceSuccessMessage = '';

    if (!this.selectedMarketId || !this.selectedProductId) {
      this.priceErrorMessage = 'Select an existing market and product before submitting a price.';
      return;
    }

    this.isSubmittingPrice = true;
    this.api.post('/Prices', {
      marketId: this.selectedMarketId,
      productId: this.selectedProductId,
      unit: this.selectedUnit,
      pricePerUnit: this.price,
      quantityChecked: this.quantity,
      priceDate: this.priceDate,
      priceTime: this.priceTime ? `${this.priceTime}:00` : null,
      sellerType: this.sellerType === 'Street vendor' ? 'StreetVendor' : this.sellerType,
      priceSource: this.priceSource === 'Observed in market' ? 'ObservedInMarket' : this.priceSource === 'Seller quoted' ? 'SellerProvided' : 'UserReported',
      qualityGrade: this.quality === 'Low grade' ? 'Low' : this.quality,
      notes: this.notes || null,
    }).pipe(finalize(() => this.isSubmittingPrice = false)).subscribe({
      next: () => {
        this.priceSuccessMessage = 'Price submitted successfully.';
        this.isThankYouOpen = true;
        this.drafts.clearDraft(this.marketDraftKey);
        this.drafts.clearDraft(this.productDraftKey);
        this.drafts.clearDraft(this.priceDraftKey);
        this.loadTodayPrices();
      },
      error: error => {
        this.priceErrorMessage = error instanceof Error ? error.message : 'Unable to submit price.';
      },
    });
  }

  closeModal(): void {
    this.isThankYouOpen = false;
  }

  saveDraft(): void {
    this.persistDraftIfChanged(true);
    this.priceErrorMessage = '';
  }

  private updateSeo(): void {
    this.translate
      .get([
        'prices.seo.title',
        'prices.seo.description',
        'prices.seo.keywords',
        'prices.seo.ogTitle',
        'prices.seo.ogDescription',
      ])
      .subscribe((translations) => {
        const title = translations['prices.seo.title'];
        const description = translations['prices.seo.description'];
        const keywords = translations['prices.seo.keywords'];
        const ogTitle = translations['prices.seo.ogTitle'];
        const ogDescription = translations['prices.seo.ogDescription'];

        this.title.setTitle(title);
        this.meta.updateTag({ name: 'description', content: description });
        this.meta.updateTag({ name: 'keywords', content: keywords });
        this.meta.updateTag({ name: 'robots', content: 'index, follow' });
        this.meta.updateTag({ property: 'og:title', content: ogTitle });
        this.meta.updateTag({ property: 'og:description', content: ogDescription });
        this.meta.updateTag({ property: 'og:type', content: 'website' });
        this.meta.updateTag({ property: 'og:url', content: this.pageUrl });
        this.meta.updateTag({ property: 'og:image', content: this.ogImageUrl });
        this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
        this.meta.updateTag({ name: 'twitter:title', content: ogTitle });
        this.meta.updateTag({ name: 'twitter:description', content: ogDescription });
        this.meta.updateTag({ name: 'twitter:image', content: this.ogImageUrl });
        this.setCanonicalUrl();
        this.setJsonLd();
      });
  }

  private loadPageData(): void {
    this.priceErrorMessage = '';
    this.loadDivisions();
    this.loadDistricts();
    this.loadUpazilas();
    this.loadUnionOrWards();
    this.loadCategories();
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  private restoreDraft(): void {
    const draft = this.drafts.getDraft<Partial<{
      selectedDivisionId: string;
      selectedDistrictId: string;
      selectedUpazilaId: string;
      selectedUnionOrWardId: string;
      selectedMarketId: string;
      selectedProductId: string;
      selectedCategoryId: string;
      selectedUnit: string;
      price: number;
      quantity: number;
      priceDate: string;
      priceTime: string;
      sellerType: string;
      priceSource: string;
      quality: string;
      notes: string;
      marketSearch: string;
      productSearch: string;
    }>>(this.priceDraftKey);

    if (!draft) {
      return;
    }

    this.selectedDivisionId = draft.selectedDivisionId ?? '';
    this.selectedDistrictId = draft.selectedDistrictId ?? '';
    this.selectedUpazilaId = draft.selectedUpazilaId ?? '';
    this.selectedUnionOrWardId = draft.selectedUnionOrWardId ?? '';
    this.selectedMarketId = draft.selectedMarketId ?? '';
    this.selectedProductId = draft.selectedProductId ?? '';
    this.selectedCategoryId = draft.selectedCategoryId ?? '';
    this.selectedUnit = draft.selectedUnit ?? 'kg';
    this.price = draft.price ?? 0;
    this.quantity = draft.quantity ?? 1;
    this.priceDate = draft.priceDate ?? new Date().toISOString().slice(0, 10);
    this.priceTime = draft.priceTime ?? '09:30';
    this.sellerType = draft.sellerType ?? 'Retail';
    this.priceSource = draft.priceSource ?? 'Observed in market';
    this.quality = draft.quality ?? 'Standard';
    this.notes = draft.notes ?? '';
    this.marketSearch = draft.marketSearch ?? '';
    this.productSearch = draft.productSearch ?? '';
    this.lastDraftJson = JSON.stringify(this.getDraftData());
  }

  private persistDraftIfChanged(force = false): void {
    const draft = this.getDraftData();
    const nextDraftJson = JSON.stringify(draft);

    if (!force && nextDraftJson === this.lastDraftJson) {
      return;
    }

    this.lastDraftJson = nextDraftJson;
    this.drafts.saveDraft(this.priceDraftKey, draft);
  }

  private getDraftData(): object {
    return {
      selectedDivisionId: this.selectedDivisionId,
      selectedDistrictId: this.selectedDistrictId,
      selectedUpazilaId: this.selectedUpazilaId,
      selectedUnionOrWardId: this.selectedUnionOrWardId,
      selectedMarketId: this.selectedMarketId,
      selectedProductId: this.selectedProductId,
      selectedCategoryId: this.selectedCategoryId,
      selectedUnit: this.selectedUnit,
      price: this.price,
      quantity: this.quantity,
      priceDate: this.priceDate,
      priceTime: this.priceTime,
      sellerType: this.sellerType,
      priceSource: this.priceSource,
      quality: this.quality,
      notes: this.notes,
      marketSearch: this.marketSearch,
      productSearch: this.productSearch,
    };
  }

  loadDivisions(): void {
    this.isLoadingDivisions = true;
    this.api.get<LocationResponse[]>('/locations/divisions')
      .pipe(finalize(() => this.isLoadingDivisions = false))
      .subscribe({
        next: divisions => this.divisions = divisions,
        error: error => this.locationErrorMessage = error instanceof Error ? error.message : 'Unable to load divisions.',
      });
  }

  loadDistricts(): void {
    if (!this.selectedDivisionId) {
      return;
    }

    this.isLoadingDistricts = true;
    this.api.get<LocationResponse[]>('/locations/districts', { divisionId: this.selectedDivisionId })
      .pipe(finalize(() => this.isLoadingDistricts = false))
      .subscribe({
        next: districts => this.districts = districts,
        error: error => this.locationErrorMessage = error instanceof Error ? error.message : 'Unable to load districts.',
      });
  }

  loadUpazilas(): void {
    if (!this.selectedDistrictId) {
      return;
    }

    this.isLoadingUpazilas = true;
    this.api.get<LocationResponse[]>('/locations/upazilas', { districtId: this.selectedDistrictId })
      .pipe(finalize(() => this.isLoadingUpazilas = false))
      .subscribe({
        next: upazilas => this.upazilas = upazilas,
        error: error => this.locationErrorMessage = error instanceof Error ? error.message : 'Unable to load upazilas.',
      });
  }

  loadUnionOrWards(): void {
    if (!this.selectedUpazilaId) {
      return;
    }

    this.isLoadingUnionOrWards = true;
    this.api.get<LocationResponse[]>('/locations/unions-or-wards', { upazilaId: this.selectedUpazilaId })
      .pipe(finalize(() => this.isLoadingUnionOrWards = false))
      .subscribe({
        next: unionOrWards => this.unionOrWards = unionOrWards,
        error: error => this.locationErrorMessage = error instanceof Error ? error.message : 'Unable to load unions/wards.',
      });
  }

  loadMarkets(): void {
    this.isLoadingMarkets = true;
    this.api.get<MarketResponse[]>('/Markets', {
      divisionId: this.selectedDivisionId,
      districtId: this.selectedDistrictId,
      upazilaId: this.selectedUpazilaId,
      unionOrWardId: this.selectedUnionOrWardId,
      search: this.marketSearch,
      pageNumber: 1,
      pageSize: 20,
    }).pipe(finalize(() => this.isLoadingMarkets = false)).subscribe({
      next: markets => this.markets = markets,
      error: error => this.priceErrorMessage = error instanceof Error ? error.message : 'Unable to load markets.',
    });
  }

  loadCategories(): void {
    this.isLoadingCategories = true;
    this.api.get<ProductCategoryResponse[]>('/product-categories')
      .pipe(finalize(() => this.isLoadingCategories = false))
      .subscribe({
        next: categories => {
          this.categories = categories;
          if (!this.selectedCategoryId) {
            this.selectedCategoryId = categories[0]?.id ?? '';
          }
          this.loadProducts();
        },
        error: error => this.productErrorMessage = error instanceof Error ? error.message : 'Unable to load product categories.',
      });
  }

  loadProducts(): void {
    this.isLoadingProducts = true;
    this.api.get<ProductResponse[]>('/Products', {
      categoryId: this.selectedCategoryId,
      search: this.productSearch,
      pageNumber: 1,
      pageSize: 20,
    }).pipe(finalize(() => this.isLoadingProducts = false)).subscribe({
      next: products => {
        this.products = products;
        const selectedProduct = products.find(product => product.id === this.selectedProductId);
        if (!selectedProduct) {
          this.selectedProductId = products[0]?.id ?? '';
        }
        const product = this.products.find(item => item.id === this.selectedProductId);
        this.selectedUnit = product?.primaryUnit ?? this.selectedUnit;
        this.loadPriceSummary();
      },
      error: error => this.productErrorMessage = error instanceof Error ? error.message : 'Unable to load products.',
    });
  }

  private loadTodayPrices(): void {
    this.isLoadingPrices = true;
    this.api.get<PriceSubmissionResponse[]>('/Prices/today', {
      divisionId: this.selectedDivisionId,
      districtId: this.selectedDistrictId,
      upazilaId: this.selectedUpazilaId,
      unionOrWardId: this.selectedUnionOrWardId,
      marketId: this.selectedMarketId,
      categoryId: this.selectedCategoryId,
      productId: this.selectedProductId,
    }).pipe(finalize(() => this.isLoadingPrices = false)).subscribe({
      next: prices => {
        this.todayPrices = prices;
      },
      error: error => {
        this.priceErrorMessage = error instanceof Error ? error.message : 'Unable to load prices.';
      },
    });
  }

  private loadPriceSummary(): void {
    this.isLoadingSummary = true;
    this.api.get<PriceSummaryResponse>('/Prices/summary', {
      divisionId: this.selectedDivisionId,
      districtId: this.selectedDistrictId,
      upazilaId: this.selectedUpazilaId,
      unionOrWardId: this.selectedUnionOrWardId,
      marketId: this.selectedMarketId,
      categoryId: this.selectedCategoryId,
      productId: this.selectedProductId,
    }).pipe(finalize(() => this.isLoadingSummary = false)).subscribe({
      next: summary => this.priceSummary = summary,
      error: () => this.priceSummary = undefined,
    });
  }

  getLocationName(location?: LocationResponse): string {
    if (!location) {
      return '';
    }

    return this.currentLanguage === 'bn' ? location.nameBn : location.nameEn;
  }

  getCategoryName(category?: ProductCategoryResponse): string {
    if (!category) {
      return '';
    }

    return this.currentLanguage === 'bn' ? category.nameBn : category.nameEn;
  }

  getProductName(product: ProductResponse): string {
    return this.currentLanguage === 'bn' ? product.nameBn : product.nameEn;
  }

  private setCanonicalUrl(): void {
    let canonical = this.document.querySelector<HTMLLinkElement>('link[rel="canonical"]');

    if (!canonical) {
      canonical = this.document.createElement('link');
      canonical.setAttribute('rel', 'canonical');
      this.document.head.appendChild(canonical);
    }

    canonical.setAttribute('href', this.pageUrl);
  }

  private setJsonLd(): void {
    const schema = {
      '@context': 'https://schema.org',
      '@graph': [
        {
          '@type': 'WebPage',
          '@id': `${this.pageUrl}#webpage`,
          url: this.pageUrl,
          name: this.translate.instant('prices.seo.title'),
          description: this.translate.instant('prices.seo.description'),
          inLanguage: this.translate.currentLang === 'bn' ? 'bn-BD' : 'en-BD',
          isPartOf: {
            '@id': `${this.siteUrl}/#website`,
          },
          about: [
            { '@type': 'Thing', name: 'today’s bazar price Bangladesh' },
            { '@type': 'Thing', name: 'submit bazar price' },
            { '@type': 'Thing', name: 'current market price Bangladesh' },
            { '@type': 'Thing', name: 'Bangladesh market price update' },
            { '@type': 'Thing', name: 'reliable bazar price' },
          ],
          mainEntity: {
            '@id': `${this.pageUrl}#faq`,
          },
        },
        {
          '@type': 'BreadcrumbList',
          '@id': `${this.pageUrl}#breadcrumb`,
          itemListElement: [
            {
              '@type': 'ListItem',
              position: 1,
              name: this.translate.instant('nav.home'),
              item: `${this.siteUrl}/home`,
            },
            {
              '@type': 'ListItem',
              position: 2,
              name: this.translate.instant('nav.prices'),
              item: this.pageUrl,
            },
          ],
        },
        {
          '@type': 'FAQPage',
          '@id': `${this.pageUrl}#faq`,
          mainEntity: [
            this.buildFaqSchema('prices.faq.q1.question', 'prices.faq.q1.answer'),
            this.buildFaqSchema('prices.faq.q2.question', 'prices.faq.q2.answer'),
            this.buildFaqSchema('prices.faq.q3.question', 'prices.faq.q3.answer'),
            this.buildFaqSchema('prices.faq.q4.question', 'prices.faq.q4.answer'),
          ],
        },
        {
          '@type': 'Dataset',
          '@id': `${this.pageUrl}#dataset`,
          name: this.translate.instant('prices.schema.datasetName'),
          description: this.translate.instant('prices.schema.datasetDescription'),
          creator: {
            '@id': `${this.siteUrl}/#organization`,
          },
          spatialCoverage: {
            '@type': 'Country',
            name: 'Bangladesh',
          },
          keywords: [
            'today’s bazar price Bangladesh',
            'local market price Bangladesh',
            'grocery price Bangladesh',
            'vegetable price Bangladesh',
            'fish price Bangladesh',
            'meat price Bangladesh',
            'daily essentials price Bangladesh',
          ],
        },
      ],
    };

    let script = this.document.getElementById(this.jsonLdScriptId) as HTMLScriptElement | null;

    if (!script) {
      script = this.document.createElement('script');
      script.id = this.jsonLdScriptId;
      script.type = 'application/ld+json';
      this.document.head.appendChild(script);
    }

    script.text = JSON.stringify(schema);
  }

  private buildFaqSchema(questionKey: string, answerKey: string): object {
    return {
      '@type': 'Question',
      name: this.translate.instant(questionKey),
      acceptedAnswer: {
        '@type': 'Answer',
        text: this.translate.instant(answerKey),
      },
    };
  }
}
