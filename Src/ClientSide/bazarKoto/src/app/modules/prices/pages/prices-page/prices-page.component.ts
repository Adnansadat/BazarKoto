import { CommonModule, DOCUMENT, isPlatformBrowser } from '@angular/common';
import { AfterViewChecked, AfterViewInit, ChangeDetectionStrategy, Component, DoCheck, ElementRef, Inject, OnDestroy, OnInit, PLATFORM_ID, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Meta, Title } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { finalize, Subscription } from 'rxjs';
import { Api } from '../../../../core/services/api';
import { DraftService } from '../../../../core/services/draft';
import { UserTracking } from '../../../../core/services/user-tracking';

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
  localName?: string | null;
  primaryUnit: string;
  productState: string;
  notes?: string | null;
  status?: string;
  isActive?: boolean;
}

interface PriceSubmissionResponse {
  id: string;
  marketId: string;
  productId: string;
  categoryId: string;
  productNameEn: string;
  productNameBn: string;
  marketName: string;
  pricePerUnit: number;
  quantityChecked?: number | null;
  unit: string;
  priceDate: string;
  priceTime?: string | null;
  sellerType: string;
  priceSource: string;
  qualityGrade: string;
  notes?: string | null;
  trackingGuid?: string | null;
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PricesPageComponent implements AfterViewInit, AfterViewChecked, OnInit, OnDestroy, DoCheck {
  @ViewChild('marketInput') private marketInput?: ElementRef<HTMLInputElement>;
  @ViewChild('pricePerUnitInput') private pricePerUnitInput?: ElementRef<HTMLInputElement>;

  private readonly siteUrl = 'https://www.bazarkoto.com';
  private readonly marketDraftKey = 'bazarkoto.market.draft';
  private readonly productDraftKey = 'bazarkoto.product.draft';
  private readonly priceDraftKey = 'bazarkoto.price.draft';
  private readonly pageUrl = `${this.siteUrl}/prices`;
  private readonly ogImageUrl = `${this.siteUrl}/images/bazar-hero.png`;
  private readonly jsonLdScriptId = 'prices-page-json-ld';
  private readonly maxPostInitFocusChecks = 12;
  private lastDraftJson = '';
  private langChangeSubscription?: Subscription;
  private initialPriceInputFocusChecks = 0;

  selectedDivisionId = signal('');
  selectedDistrictId = signal('');
  selectedUpazilaId = signal('');
  selectedUnionOrWardId = signal('');
  selectedMarketId = signal('');
  selectedProductId = signal('');
  selectedCategoryId = signal('');
  existingPriceId = signal('');
  loadedExistingPricePerUnit = signal<number | null>(null);
  selectedUnit = signal('kg');
  price = signal(0);
  quantity = signal(1);
  priceDate = signal(new Date().toISOString().slice(0, 10));
  priceTime = signal('09:30');
  sellerType = signal('Retail');
  priceSource = signal('Observed in market');
  quality = signal('Standard');
  notes = signal('');
  isPricePerUnitInputActive = signal(false);
  isThankYouOpen = signal(false);
  isUpdateSuccessOpen = signal(false);
  showPriceValidation = signal(false);
  isLoadingPrices = signal(true);
  isLoadingDivisions = signal(false);
  isLoadingDistricts = signal(false);
  isLoadingUpazilas = signal(false);
  isLoadingUnionOrWards = signal(false);
  isLoadingMarkets = signal(false);
  isLoadingCategories = signal(false);
  isLoadingProducts = signal(false);
  isLoadingSummary = signal(false);
  isLoadingExistingPrice = signal(false);
  isSubmittingPrice = signal(false);
  priceErrorMessage = signal('');
  priceSuccessMessage = signal('');
  locationErrorMessage = signal('');
  productErrorMessage = signal('');
  modalProductName = signal('');
  modalMarketName = signal('');
  marketSearch = signal('');
  productSearch = signal('');
  divisions = signal<LocationResponse[]>([]);
  districts = signal<LocationResponse[]>([]);
  upazilas = signal<LocationResponse[]>([]);
  unionOrWards = signal<LocationResponse[]>([]);
  categories = signal<ProductCategoryResponse[]>([]);
  markets = signal<MarketResponse[]>([]);
  products = signal<ProductResponse[]>([]);
  todayPrices = signal<PriceSubmissionResponse[]>([]);
  priceSummary = signal<PriceSummaryResponse | undefined>(undefined);

  constructor(
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly translate: TranslateService,
    private readonly router: Router,
    private readonly api: Api,
    private readonly drafts: DraftService,
    private readonly userTracking: UserTracking,
    @Inject(DOCUMENT) private readonly document: Document,
    @Inject(PLATFORM_ID) private readonly platformId: object,
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
    this.restoreMarketDraftContext();
    this.loadPageData();
    this.langChangeSubscription = this.translate.onLangChange.subscribe(() => this.updateSeo());
  }

  ngAfterViewInit(): void {
    if (!this.isBrowser) {
      return;
    }

    setTimeout(() => this.resetScrollAndFocusPriceInput());
  }

  ngAfterViewChecked(): void {
    if (!this.isBrowser) {
      return;
    }

    if (this.initialPriceInputFocusChecks >= this.maxPostInitFocusChecks) {
      return;
    }

    this.initialPriceInputFocusChecks += 1;
    const element = this.pricePerUnitInput?.nativeElement;

    if (!element) {
      return;
    }

    if (this.document.activeElement !== element) {
      this.initialPriceInputFocusChecks = this.maxPostInitFocusChecks;
      setTimeout(() => this.resetScrollAndFocusPriceInput());
      return;
    }

    this.initialPriceInputFocusChecks = this.maxPostInitFocusChecks;
    setTimeout(() => {
      this.isPricePerUnitInputActive.set(true);
    });
  }

  ngOnDestroy(): void {
    this.langChangeSubscription?.unsubscribe();
    this.document.getElementById(this.jsonLdScriptId)?.remove();
  }

  ngDoCheck(): void {
    this.persistDraftIfChanged();
  }

  private get isBrowser(): boolean {
    return isPlatformBrowser(this.platformId);
  }

  get totalPrice(): number {
    return this.price() * this.quantity();
  }

  get selectedUnitLabelKey(): string {
    return this.units.find((unit) => unit.value === this.selectedUnit())?.labelKey ?? '';
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.translate.defaultLang || 'en';
  }

  get selectedMarketName(): string {
    return this.markets().find(market => market.id === this.selectedMarketId())?.marketName
      ?? this.getStoredSelectedMarketName()
      ?? '';
  }

  get selectedProductName(): string {
    const product = this.products().find(item => item.id === this.selectedProductId());
    return product ? this.getProductName(product) : this.getStoredSelectedProductName();
  }

  get selectedCategoryName(): string {
    const category = this.categories().find(item => item.id === this.selectedCategoryId());
    return category ? this.getCategoryName(category) : '';
  }

  get reviewSegments(): string[] {
    const segments = [
      this.selectedMarketName,
      this.selectedProductName,
      this.price() > 0 ? `৳ ${this.price()}/${this.selectedUnit()}` : '',
      this.quantity() ? `${this.quantity()} ${this.selectedUnit()}` : '',
      this.totalPrice > 0 ? `৳ ${this.totalPrice}` : '',
    ];

    const priceSummary = this.priceSummary();

    if (priceSummary) {
      segments.push(
        `Avg ৳ ${priceSummary.averagePrice}/${priceSummary.unit || this.selectedUnit()}`,
        `Min ৳ ${priceSummary.minimumPrice}`,
        `Max ৳ ${priceSummary.maximumPrice}`,
        `${priceSummary.submissionCount} submissions`,
      );
    }

    return segments.filter(Boolean);
  }

  get isUpdateMode(): boolean {
    return Boolean(this.existingPriceId());
  }

  get hasLoadedExistingPrice(): boolean {
    return this.loadedExistingPricePerUnit() !== null;
  }

  get hasPriceChanged(): boolean {
    return this.hasLoadedExistingPrice && Number(this.price()) !== Number(this.loadedExistingPricePerUnit());
  }

  get marketContextInvalid(): boolean {
    return this.showPriceValidation() && !this.hasSelectedMarketContext;
  }

  get productContextInvalid(): boolean {
    return this.showPriceValidation() && !this.hasSelectedProductContext;
  }

  get priceInvalid(): boolean {
    return Number(this.price()) <= 0;
  }

  get quantityInvalid(): boolean {
    return this.showPriceValidation() && this.quantity() <= 0;
  }

  get sellerTypeInvalid(): boolean {
    return this.showPriceValidation() && !this.sellerType();
  }

  get priceSourceInvalid(): boolean {
    return this.showPriceValidation() && !this.priceSource();
  }

  get qualityInvalid(): boolean {
    return this.showPriceValidation() && !this.quality();
  }

  get canAttemptSubmitPrice(): boolean {
    return !this.isSubmittingPrice() && !this.isLoadingExistingPrice() && (!this.hasLoadedExistingPrice || this.hasPriceChanged);
  }

  get canSubmitPrice(): boolean {
    return this.canAttemptSubmitPrice && this.isPriceFormValid();
  }

  focusSubmissionForm(): void {
    if (!this.isBrowser) {
      return;
    }

    this.focusPricePerUnitInput();
    this.pricePerUnitInput?.nativeElement.scrollIntoView?.({ behavior: 'smooth', block: 'center' });
  }

  onPricePerUnitInputFocus(): void {
    this.isPricePerUnitInputActive.set(true);
  }

  onPricePerUnitInputBlur(): void {
    this.isPricePerUnitInputActive.set(false);
  }

  private focusPricePerUnitInput(): void {
    if (!this.isBrowser) {
      return;
    }

    const element = this.pricePerUnitInput?.nativeElement;

    if (!element) {
      return;
    }

    element.scrollIntoView?.({ behavior: 'auto', block: 'center' });
    element.focus?.();
    this.isPricePerUnitInputActive.set(this.document.activeElement === element);
  }

  private resetScrollAndFocusPriceInput(): void {
    this.focusPricePerUnitInput();
  }

  onDivisionChange(): void {
    this.selectedDistrictId.set('');
    this.selectedUpazilaId.set('');
    this.selectedUnionOrWardId.set('');
    this.selectedMarketId.set('');
    this.districts.set([]);
    this.upazilas.set([]);
    this.unionOrWards.set([]);
    this.markets.set([]);
    this.loadDistricts();
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onDistrictChange(): void {
    this.selectedUpazilaId.set('');
    this.selectedUnionOrWardId.set('');
    this.selectedMarketId.set('');
    this.upazilas.set([]);
    this.unionOrWards.set([]);
    this.markets.set([]);
    this.loadUpazilas();
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onUpazilaChange(): void {
    this.selectedUnionOrWardId.set('');
    this.selectedMarketId.set('');
    this.unionOrWards.set([]);
    this.markets.set([]);
    this.loadUnionOrWards();
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onUnionOrWardChange(): void {
    this.selectedMarketId.set('');
    this.loadMarkets();
    this.loadTodayPrices();
    this.loadPriceSummary();
  }

  onMarketChange(): void {
    this.clearLoadedExistingPrice();
    this.loadTodayPrices();
    this.loadPriceSummary();
    this.loadExistingPriceIfReady();
  }

  onCategoryChange(): void {
    this.selectedProductId.set('');
    this.clearLoadedExistingPrice();
    this.products.set([]);
    this.loadProducts();
    this.loadTodayPrices();
  }

  onProductChange(): void {
    const product = this.products().find(item => item.id === this.selectedProductId());
    this.selectedUnit.set(product?.primaryUnit ?? this.selectedUnit());
    this.clearLoadedExistingPrice();
    this.loadTodayPrices();
    this.loadPriceSummary();
    this.loadExistingPriceIfReady();
  }

  submitPrice(allowMarketResolveRetry = true): void {
    this.showPriceValidation.set(true);
    this.priceErrorMessage.set('');
    this.priceSuccessMessage.set('');
    this.applyCurrentSubmissionTime();
    this.resolveSelectedMarketIdFromLoadedMarkets();

    if (!this.hasSelectedMarketContext || !this.hasSelectedProductContext) {
      this.priceErrorMessage.set('Complete market and product selection before submitting a price.');
      return;
    }

    if (!this.selectedMarketId() || !this.selectedProductId()) {
      this.priceErrorMessage.set('Market and product are selected, but their database records could not be loaded. Please start the backend and reload this page before submitting.');
      return;
    }

    if (!this.isPriceFormValid()) {
      this.priceErrorMessage.set('Please complete all required price fields.');
      return;
    }

    if (!this.canAttemptSubmitPrice) {
      if (this.hasLoadedExistingPrice && !this.hasPriceChanged) {
        this.priceErrorMessage.set('Change the price per unit before submitting an update.');
      }
      return;
    }

    if (this.existingPriceId()) {
      this.updateExistingPrice();
      return;
    }

    this.isSubmittingPrice.set(true);
    const trackingContext = this.getSubmissionTrackingContext();

    this.api.post<PriceSubmissionResponse>('/Prices', {
      marketId: this.selectedMarketId(),
      productId: this.selectedProductId(),
      unit: this.selectedUnit(),
      pricePerUnit: this.price(),
      quantityChecked: this.quantity(),
      priceDate: this.priceDate(),
      priceTime: this.priceTime() ? `${this.priceTime()}:00` : null,
      sellerType: this.sellerType() === 'Street vendor' ? 'StreetVendor' : this.sellerType(),
      priceSource: this.priceSource() === 'Observed in market' ? 'ObservedInMarket' : this.priceSource() === 'Seller quoted' ? 'SellerProvided' : 'UserReported',
      qualityGrade: this.quality() === 'Low grade' ? 'Low' : this.quality(),
      notes: this.notes() || null,
      trackingGuid: trackingContext.trackingGuid,
      gpsPermissionStatus: trackingContext.gpsPermissionStatus,
      gpsLatitude: trackingContext.gpsLatitude,
      gpsLongitude: trackingContext.gpsLongitude,
      gpsAccuracyMeters: trackingContext.gpsAccuracyMeters,
      locationSource: trackingContext.locationSource,
    }).pipe(finalize(() => this.isSubmittingPrice.set(false))).subscribe({
      next: response => {
        this.userTracking.saveTrackingGuid(response.trackingGuid);
        this.priceSuccessMessage.set('Price submitted successfully.');
        this.showPriceValidation.set(false);
        this.captureSuccessModalContext();
        this.isThankYouOpen.set(true);
        this.loadExistingPriceIfReady();
        this.clearSubmissionDrafts();
        this.loadTodayPrices();
      },
      error: error => {
        const errorMessage = error instanceof Error ? error.message : 'Unable to submit price.';
        this.priceErrorMessage.set(errorMessage);

        if (this.isExistingPriceError(errorMessage)) {
          this.loadExistingPriceIfReady();
          return;
        }

        if (allowMarketResolveRetry && this.isMissingMarketError(errorMessage) && this.resolveSelectedMarketIdFromLoadedMarkets()) {
          this.submitPrice(false);
        }
      },
    });
  }

  private getSubmissionTrackingContext(): {
    trackingGuid: string;
    gpsPermissionStatus: string | null;
    gpsLatitude: number | null;
    gpsLongitude: number | null;
    gpsAccuracyMeters: number | null;
    locationSource: string;
  } {
    const trackingGuid = this.userTracking.getOrCreateTrackingGuid();
    const gpsSnapshot = this.userTracking.getBrowserGpsSnapshot();
    const hasGpsCoordinates = gpsSnapshot.gpsPermissionStatus === 'granted'
      && gpsSnapshot.gpsLatitude !== null
      && gpsSnapshot.gpsLatitude !== undefined
      && gpsSnapshot.gpsLongitude !== null
      && gpsSnapshot.gpsLongitude !== undefined;

    return {
      trackingGuid,
      gpsPermissionStatus: gpsSnapshot.gpsPermissionStatus,
      gpsLatitude: gpsSnapshot.gpsLatitude ?? null,
      gpsLongitude: gpsSnapshot.gpsLongitude ?? null,
      gpsAccuracyMeters: gpsSnapshot.gpsAccuracyMeters ?? null,
      locationSource: hasGpsCoordinates ? 'gps' : 'market',
    };
  }

  loadExistingPrice(): void {
    this.priceErrorMessage.set('');
    this.priceSuccessMessage.set('');

    if (!this.selectedMarketId() || !this.selectedProductId()) {
      this.priceErrorMessage.set('Select an existing market and product before loading a price.');
      return;
    }

    this.isLoadingExistingPrice.set(true);
    this.api.get<PriceSubmissionResponse>('/Prices/latest', {
      marketId: this.selectedMarketId(),
      productId: this.selectedProductId(),
    }).pipe(finalize(() => this.isLoadingExistingPrice.set(false))).subscribe({
      next: existingPrice => {
        this.existingPriceId.set(existingPrice.id);
        this.selectedUnit.set(existingPrice.unit || this.selectedUnit());
        this.price.set(existingPrice.pricePerUnit);
        this.quantity.set(existingPrice.quantityChecked ?? this.quantity());
        this.priceDate.set(existingPrice.priceDate || this.priceDate());
        this.priceTime.set(existingPrice.priceTime ? existingPrice.priceTime.slice(0, 5) : this.priceTime());
        this.sellerType.set(this.toDisplaySellerType(existingPrice.sellerType));
        this.priceSource.set(this.toDisplayPriceSource(existingPrice.priceSource));
        this.quality.set(this.toDisplayQuality(existingPrice.qualityGrade));
        this.notes.set(existingPrice.notes ?? '');
        this.loadedExistingPricePerUnit.set(existingPrice.pricePerUnit);
        this.priceSuccessMessage.set('Existing price loaded. Only the price per unit will be updated.');
      },
      error: () => {
        this.clearLoadedExistingPrice();
        this.price.set(0);
        this.priceSuccessMessage.set('');
      },
    });
  }

  private updateExistingPrice(): void {
    const existingPriceId = this.existingPriceId();

    if (!existingPriceId) {
      this.priceErrorMessage.set('Load the existing market product price before saving an update.');
      return;
    }

    if (this.price() <= 0) {
      this.priceErrorMessage.set('Price per unit must be greater than zero.');
      return;
    }

    this.isSubmittingPrice.set(true);
    this.api.put(`/Prices/${existingPriceId}`, {
      marketId: this.selectedMarketId(),
      productId: this.selectedProductId(),
      unit: this.selectedUnit(),
      pricePerUnit: this.price(),
      quantityChecked: this.quantity(),
      priceDate: this.priceDate(),
      priceTime: this.priceTime() ? `${this.priceTime()}:00` : null,
      sellerType: this.sellerType() === 'Street vendor' ? 'StreetVendor' : this.sellerType(),
      priceSource: this.priceSource() === 'Observed in market' ? 'ObservedInMarket' : this.priceSource() === 'Seller quoted' ? 'SellerProvided' : 'UserReported',
      qualityGrade: this.quality() === 'Low grade' ? 'Low' : this.quality(),
      notes: this.notes() || null,
    }).pipe(finalize(() => this.isSubmittingPrice.set(false))).subscribe({
      next: () => {
        this.priceSuccessMessage.set('');
        this.priceErrorMessage.set('');
        this.showPriceValidation.set(false);
        this.loadedExistingPricePerUnit.set(this.price());
        this.captureSuccessModalContext();
        this.isUpdateSuccessOpen.set(true);
        this.loadTodayPrices();
        this.loadPriceSummary();
      },
      error: error => {
        this.priceErrorMessage.set(error instanceof Error ? error.message : 'Unable to update existing price.');
      },
    });
  }

  private loadExistingPriceIfReady(): void {
    if (this.selectedMarketId() && this.selectedProductId()) {
      this.loadExistingPrice();
    }
  }

  private clearLoadedExistingPrice(): void {
    this.existingPriceId.set('');
    this.loadedExistingPricePerUnit.set(null);
  }

  private captureSuccessModalContext(): void {
    this.modalProductName.set(this.selectedProductName);
    this.modalMarketName.set(this.selectedMarketName);
  }

  private isPriceFormValid(): boolean {
    return Boolean(
      this.hasSelectedMarketContext &&
      this.hasSelectedProductContext &&
      this.price() > 0 &&
      this.quantity() > 0 &&
      this.sellerType() &&
      this.priceSource() &&
      this.quality()
    );
  }

  private get hasSelectedMarketContext(): boolean {
    return Boolean(this.selectedMarketId() || this.selectedMarketName.trim());
  }

  private get hasSelectedProductContext(): boolean {
    return Boolean(this.selectedProductId() || this.selectedProductName.trim());
  }

  private toDisplaySellerType(value: string): string {
    return value === 'StreetVendor' ? 'Street vendor' : value || 'Retail';
  }

  private isExistingPriceError(message: string): boolean {
    return message.toLowerCase().includes('price already exists');
  }

  private isMissingMarketError(message: string): boolean {
    return message.toLowerCase().includes('selected market was not found');
  }

  private toDisplayPriceSource(value: string): string {
    if (value === 'ObservedInMarket') {
      return 'Observed in market';
    }

    if (value === 'SellerProvided') {
      return 'Seller quoted';
    }

    return 'Purchased personally';
  }

  private toDisplayQuality(value: string): string {
    return value === 'Low' ? 'Low grade' : value || 'Standard';
  }

  closeModal(): void {
    this.isThankYouOpen.set(false);
    this.router.navigate(['/home']);
  }

  closeUpdateSuccessModal(): void {
    this.isUpdateSuccessOpen.set(false);
    this.clearSubmissionDrafts();
    this.router.navigate(['/home']);
  }

  private clearSubmissionDrafts(): void {
    this.drafts.clearDraft(this.marketDraftKey);
    this.drafts.clearDraft(this.productDraftKey);
    this.drafts.clearDraft(this.priceDraftKey);
  }

  saveDraft(): void {
    this.persistDraftIfChanged(true);
    this.priceErrorMessage.set('');
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
    this.priceErrorMessage.set('');
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

    this.selectedDivisionId.set(draft.selectedDivisionId ?? '');
    this.selectedDistrictId.set(draft.selectedDistrictId ?? '');
    this.selectedUpazilaId.set(draft.selectedUpazilaId ?? '');
    this.selectedUnionOrWardId.set(draft.selectedUnionOrWardId ?? '');
    this.selectedMarketId.set(draft.selectedMarketId ?? '');
    this.selectedProductId.set(draft.selectedProductId ?? '');
    this.selectedCategoryId.set(draft.selectedCategoryId ?? '');
    this.selectedUnit.set(draft.selectedUnit ?? 'kg');
    this.price.set(draft.price ?? 0);
    this.quantity.set(draft.quantity ?? 1);
    this.priceDate.set(draft.priceDate ?? new Date().toISOString().slice(0, 10));
    this.priceTime.set(draft.priceTime ?? '09:30');
    this.sellerType.set(draft.sellerType ?? 'Retail');
    this.priceSource.set(draft.priceSource ?? 'Observed in market');
    this.quality.set(draft.quality ?? 'Standard');
    this.notes.set(draft.notes ?? '');
    this.marketSearch.set(draft.marketSearch ?? '');
    this.productSearch.set(draft.productSearch ?? '');
    this.lastDraftJson = JSON.stringify(this.getDraftData());
  }

  private restoreMarketDraftContext(): void {
    const marketDraft = this.drafts.getDraft<Partial<{
      selectedDivisionId: string;
      selectedDistrictId: string;
      selectedUpazilaId: string;
      selectedUnionOrWardId: string;
      selectedMarketId: string;
      selectedMarket: string;
    }>>(this.marketDraftKey);

    if (!marketDraft) {
      return;
    }

    this.selectedDivisionId.set(marketDraft.selectedDivisionId ?? '');
    this.selectedDistrictId.set(marketDraft.selectedDistrictId ?? '');
    this.selectedUpazilaId.set(marketDraft.selectedUpazilaId ?? '');
    this.selectedUnionOrWardId.set(marketDraft.selectedUnionOrWardId ?? '');
    this.selectedMarketId.set(marketDraft.selectedMarketId ?? '');
    this.marketSearch.set(marketDraft.selectedMarket ?? '');
    this.clearLoadedExistingPrice();
    this.persistDraftIfChanged(true);
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
      selectedDivisionId: this.selectedDivisionId(),
      selectedDistrictId: this.selectedDistrictId(),
      selectedUpazilaId: this.selectedUpazilaId(),
      selectedUnionOrWardId: this.selectedUnionOrWardId(),
      selectedMarketId: this.selectedMarketId(),
      selectedProductId: this.selectedProductId(),
      selectedCategoryId: this.selectedCategoryId(),
      selectedUnit: this.selectedUnit(),
      price: this.price(),
      quantity: this.quantity(),
      priceDate: this.priceDate(),
      priceTime: this.priceTime(),
      sellerType: this.sellerType(),
      priceSource: this.priceSource(),
      quality: this.quality(),
      notes: this.notes(),
      marketSearch: this.marketSearch(),
      productSearch: this.productSearch(),
    };
  }

  loadDivisions(): void {
    this.isLoadingDivisions.set(true);
    this.api.get<LocationResponse[]>('/locations/divisions')
      .pipe(finalize(() => this.isLoadingDivisions.set(false)))
      .subscribe({
        next: divisions => this.divisions.set(divisions),
        error: error => this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load divisions.'),
      });
  }

  loadDistricts(): void {
    if (!this.selectedDivisionId()) {
      return;
    }

    this.isLoadingDistricts.set(true);
    this.api.get<LocationResponse[]>('/locations/districts', { divisionId: this.selectedDivisionId() })
      .pipe(finalize(() => this.isLoadingDistricts.set(false)))
      .subscribe({
        next: districts => this.districts.set(districts),
        error: error => this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load districts.'),
      });
  }

  loadUpazilas(): void {
    if (!this.selectedDistrictId()) {
      return;
    }

    this.isLoadingUpazilas.set(true);
    this.api.get<LocationResponse[]>('/locations/upazilas', { districtId: this.selectedDistrictId() })
      .pipe(finalize(() => this.isLoadingUpazilas.set(false)))
      .subscribe({
        next: upazilas => this.upazilas.set(upazilas),
        error: error => this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load upazilas.'),
      });
  }

  loadUnionOrWards(): void {
    if (!this.selectedUpazilaId()) {
      return;
    }

    this.isLoadingUnionOrWards.set(true);
    this.api.get<LocationResponse[]>('/locations/unions-or-wards', { upazilaId: this.selectedUpazilaId() })
      .pipe(finalize(() => this.isLoadingUnionOrWards.set(false)))
      .subscribe({
        next: unionOrWards => this.unionOrWards.set(unionOrWards),
        error: error => this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load unions/wards.'),
      });
  }

  loadMarkets(): void {
    this.isLoadingMarkets.set(true);
    this.api.get<MarketResponse[]>('/Markets', {
      divisionId: this.selectedDivisionId(),
      districtId: this.selectedDistrictId(),
      upazilaId: this.selectedUpazilaId(),
      unionOrWardId: this.selectedUnionOrWardId(),
      search: this.marketSearch(),
      pageNumber: 1,
      pageSize: 20,
    }).pipe(finalize(() => this.isLoadingMarkets.set(false))).subscribe({
      next: markets => {
        this.markets.set(markets);
        this.applyStoredMarketSelection();
      },
      error: error => this.priceErrorMessage.set(error instanceof Error ? error.message : 'Unable to load markets.'),
    });
  }

  private applyStoredMarketSelection(): void {
    if (this.markets().length === 0) {
      return;
    }

    const marketDraft = this.drafts.getDraft<Partial<{
      selectedDivisionId: string;
      selectedDistrictId: string;
      selectedUpazilaId: string;
      selectedUnionOrWardId: string;
      selectedMarketId: string;
      selectedMarket: string;
    }>>(this.marketDraftKey);

    if (this.resolveSelectedMarketIdFromLoadedMarkets()) {
      this.selectedDivisionId.set(marketDraft?.selectedDivisionId ?? this.selectedDivisionId());
      this.selectedDistrictId.set(marketDraft?.selectedDistrictId ?? this.selectedDistrictId());
      this.selectedUpazilaId.set(marketDraft?.selectedUpazilaId ?? this.selectedUpazilaId());
      this.selectedUnionOrWardId.set(marketDraft?.selectedUnionOrWardId ?? this.selectedUnionOrWardId());
      this.loadTodayPrices();
      this.loadPriceSummary();
      this.loadExistingPriceIfReady();
      return;
    }

    if (this.selectedMarketId() && this.markets().some(market => market.id === this.selectedMarketId())) {
      return;
    }

    if (marketDraft?.selectedMarketId && !this.selectedMarketId()) {
      this.selectedMarketId.set(marketDraft.selectedMarketId);
      this.selectedDivisionId.set(marketDraft.selectedDivisionId ?? this.selectedDivisionId());
      this.selectedDistrictId.set(marketDraft.selectedDistrictId ?? this.selectedDistrictId());
      this.selectedUpazilaId.set(marketDraft.selectedUpazilaId ?? this.selectedUpazilaId());
      this.selectedUnionOrWardId.set(marketDraft.selectedUnionOrWardId ?? this.selectedUnionOrWardId());
      this.loadTodayPrices();
      this.loadPriceSummary();
      this.loadExistingPriceIfReady();
      return;
    }

    if (!marketDraft?.selectedMarket) {
      return;
    }

    const normalizedStoredMarket = this.normalizeComparableText(marketDraft.selectedMarket);
    const storedMarket = this.markets().find(market => this.normalizeComparableText(market.marketName) === normalizedStoredMarket);

    if (!storedMarket) {
      return;
    }

    this.selectedMarketId.set(storedMarket.id);
    this.selectedDivisionId.set(marketDraft.selectedDivisionId ?? this.selectedDivisionId());
    this.selectedDistrictId.set(marketDraft.selectedDistrictId ?? this.selectedDistrictId());
    this.selectedUpazilaId.set(marketDraft.selectedUpazilaId ?? this.selectedUpazilaId());
    this.selectedUnionOrWardId.set(marketDraft.selectedUnionOrWardId ?? this.selectedUnionOrWardId());
    this.loadTodayPrices();
    this.loadPriceSummary();
    this.loadExistingPriceIfReady();
  }

  loadCategories(): void {
    this.isLoadingCategories.set(true);
    this.api.get<ProductCategoryResponse[]>('/product-categories')
      .pipe(finalize(() => this.isLoadingCategories.set(false)))
      .subscribe({
        next: categories => {
          this.categories.set(categories);
          const storedProduct = this.getStoredSelectedProduct();
          if (storedProduct?.categoryId) {
            this.selectedCategoryId.set(storedProduct.categoryId);
            this.selectedProductId.set(storedProduct.id);
          }
          if (!this.selectedCategoryId()) {
            this.selectedCategoryId.set(categories[0]?.id ?? '');
          }
          this.loadProducts();
        },
        error: error => this.productErrorMessage.set(error instanceof Error ? error.message : 'Unable to load product categories.'),
      });
  }

  loadProducts(): void {
    this.isLoadingProducts.set(true);
    this.api.get<ProductResponse[]>('/Products', {
      categoryId: this.selectedCategoryId(),
      search: this.productSearch(),
      pageNumber: 1,
      pageSize: 20,
    }).pipe(finalize(() => this.isLoadingProducts.set(false))).subscribe({
      next: products => {
        const storedProduct = this.getStoredSelectedProduct();
        const nextProducts = storedProduct && storedProduct.categoryId === this.selectedCategoryId() && !products.some(product => product.id === storedProduct.id)
          ? [storedProduct, ...products]
          : products;
        this.products.set(nextProducts);
        const selectedProduct = products.find(product => product.id === this.selectedProductId());
        if (!selectedProduct && storedProduct?.categoryId === this.selectedCategoryId()) {
          this.selectedProductId.set(storedProduct.id);
        } else if (!selectedProduct) {
          this.selectedProductId.set(this.products()[0]?.id ?? '');
        }
        const product = this.products().find(item => item.id === this.selectedProductId());
        this.selectedUnit.set(product?.primaryUnit ?? this.selectedUnit());
        this.loadPriceSummary();
        this.loadExistingPriceIfReady();
      },
      error: error => this.productErrorMessage.set(error instanceof Error ? error.message : 'Unable to load products.'),
    });
  }

  private getStoredSelectedProduct(): ProductResponse | null {
    if (!this.isBrowser) {
      return null;
    }

    const storedProductJson = localStorage.getItem('bazarKoto.selectedProduct');

    if (!storedProductJson) {
      return null;
    }

    try {
      const storedProduct = JSON.parse(storedProductJson) as Partial<ProductResponse>;

      if (!storedProduct.id || !storedProduct.categoryId || !storedProduct.nameEn || !storedProduct.nameBn) {
        return null;
      }

      return {
        id: storedProduct.id,
        categoryId: storedProduct.categoryId,
        categoryNameEn: storedProduct.categoryNameEn ?? '',
        categoryNameBn: storedProduct.categoryNameBn ?? '',
        nameEn: storedProduct.nameEn,
        nameBn: storedProduct.nameBn,
        localName: storedProduct.localName,
        primaryUnit: storedProduct.primaryUnit ?? this.selectedUnit(),
        productState: storedProduct.productState ?? 'Fresh',
        notes: storedProduct.notes,
        status: storedProduct.status,
        isActive: storedProduct.isActive,
      };
    } catch {
      return null;
    }
  }

  private getStoredSelectedMarketName(): string {
    return this.drafts.getDraft<Partial<{ selectedMarket: string }>>(this.marketDraftKey)?.selectedMarket?.trim() ?? '';
  }

  private resolveSelectedMarketIdFromLoadedMarkets(): boolean {
    const storedMarketName = this.getStoredSelectedMarketName();

    if (!storedMarketName || this.markets().length === 0) {
      return false;
    }

    const normalizedStoredMarket = this.normalizeComparableText(storedMarketName);
    const matchingMarket = this.markets().find(market => this.normalizeComparableText(market.marketName) === normalizedStoredMarket);

    if (!matchingMarket || matchingMarket.id === this.selectedMarketId()) {
      return false;
    }

    this.selectedMarketId.set(matchingMarket.id);
    this.selectedDivisionId.set(matchingMarket.divisionId || this.selectedDivisionId());
    this.selectedDistrictId.set(matchingMarket.districtId || this.selectedDistrictId());
    this.selectedUpazilaId.set(matchingMarket.upazilaId || this.selectedUpazilaId());
    this.selectedUnionOrWardId.set(matchingMarket.unionOrWardId || this.selectedUnionOrWardId());
    this.clearLoadedExistingPrice();
    this.persistDraftIfChanged(true);

    return true;
  }

  private getStoredSelectedProductName(): string {
    const storedProduct = this.getStoredSelectedProduct();

    if (!storedProduct) {
      return '';
    }

    return this.currentLanguage === 'bn' ? storedProduct.nameBn : storedProduct.nameEn;
  }

  private loadTodayPrices(): void {
    this.isLoadingPrices.set(true);
    this.api.get<PriceSubmissionResponse[]>('/Prices/today', {
      divisionId: this.selectedDivisionId(),
      districtId: this.selectedDistrictId(),
      upazilaId: this.selectedUpazilaId(),
      unionOrWardId: this.selectedUnionOrWardId(),
      marketId: this.selectedMarketId(),
      categoryId: this.selectedCategoryId(),
      productId: this.selectedProductId(),
      pageNumber: 1,
      pageSize: 20,
    }).pipe(finalize(() => this.isLoadingPrices.set(false))).subscribe({
      next: prices => {
        this.todayPrices.set(prices);
      },
      error: error => {
        this.priceErrorMessage.set(error instanceof Error ? error.message : 'Unable to load prices.');
      },
    });
  }

  private loadPriceSummary(): void {
    this.isLoadingSummary.set(true);
    this.api.get<PriceSummaryResponse>('/Prices/summary', {
      divisionId: this.selectedDivisionId(),
      districtId: this.selectedDistrictId(),
      upazilaId: this.selectedUpazilaId(),
      unionOrWardId: this.selectedUnionOrWardId(),
      marketId: this.selectedMarketId(),
      categoryId: this.selectedCategoryId(),
      productId: this.selectedProductId(),
    }).pipe(finalize(() => this.isLoadingSummary.set(false))).subscribe({
      next: summary => this.priceSummary.set(summary),
      error: () => this.priceSummary.set(undefined),
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

  private applyCurrentSubmissionTime(): void {
    const now = new Date();
    this.priceDate.set(this.formatDateInput(now));
    this.priceTime.set(this.formatTimeInput(now));
  }

  private formatDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private formatTimeInput(date: Date): string {
    const hours = `${date.getHours()}`.padStart(2, '0');
    const minutes = `${date.getMinutes()}`.padStart(2, '0');

    return `${hours}:${minutes}`;
  }

  private normalizeComparableText(value: string): string {
    return value.trim().toLowerCase().replace(/\s+/g, ' ');
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
