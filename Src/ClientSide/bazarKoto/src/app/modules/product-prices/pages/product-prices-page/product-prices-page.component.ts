import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { catchError, debounceTime, finalize, firstValueFrom, map, of, Subject, Subscription, switchMap, tap } from 'rxjs';
import { LocationResolver, ResolvedApproximateLocation } from '../../../../core/services/location-resolver';
import { LocationResponse, Locations } from '../../../../core/services/locations';
import { MarketOptionResponse, Markets } from '../../../../core/services/markets';
import { ProductPrices, PublicProductPriceResponse } from '../../../../core/services/product-prices';
import { ProductOptionResponse, Products } from '../../../../core/services/products';
import { BrowserGpsSnapshot, UserTracking } from '../../../../core/services/user-tracking';

type PriceCategory = 'all' | 'vegetables' | 'staples' | 'protein';
type LocationDropdown = 'division' | 'district' | 'upazila' | 'unionOrWard';

@Component({
  selector: 'app-product-prices-page',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './product-prices-page.component.html',
  styleUrl: './product-prices-page.component.scss',
})
export class ProductPricesPageComponent implements OnInit, OnDestroy {
  searchTerm = '';
  selectedCategory: PriceCategory = 'all';
  selectedDivisionId: string | null = null;
  selectedDistrictId: string | null = null;
  selectedUpazilaId: string | null = null;
  selectedUnionOrWardId: string | null = null;
  selectedMarketId: string | null = null;
  selectedMarket: MarketOptionResponse | null = null;
  selectedProductId: string | null = null;
  selectedProduct: ProductOptionResponse | null = null;
  marketSearchText = '';
  productSearchText = '';
  currentPage = 1;
  readonly pageSize = 20;
  totalCount = 0;
  serverTotalPages = 1;
  isLoading = false;
  errorMessage = '';
  locationStatusMessage = '';
  locationErrorMessage = '';
  marketErrorMessage = '';
  productStatusMessage = '';
  productErrorMessage = '';
  gpsStatusMessage = '';
  isRequestingGps = false;
  priceRows: PublicProductPriceResponse[] = [];
  divisionOptions: LocationResponse[] = [];
  districtOptions: LocationResponse[] = [];
  upazilaOptions: LocationResponse[] = [];
  unionOrWardOptions: LocationResponse[] = [];
  isLoadingDivisions = false;
  isLoadingDistricts = false;
  isLoadingUpazilas = false;
  isLoadingUnionOrWards = false;
  isLoadingMarkets = false;
  isMarketDropdownOpen = false;
  marketOptions: MarketOptionResponse[] = [];
  isLoadingProducts = false;
  isProductDropdownOpen = false;
  productOptions: ProductOptionResponse[] = [];
  openLocationDropdown: LocationDropdown | null = null;
  private readonly marketSearch$ = new Subject<string>();
  private readonly productSearch$ = new Subject<string>();
  private readonly subscriptions = new Subscription();
  private hasAttemptedSavedLocationRestore = false;

  readonly categories: Array<{ label: string; value: PriceCategory }> = [
    { label: 'All', value: 'all' },
    { label: 'Vegetables', value: 'vegetables' },
    { label: 'Staples', value: 'staples' },
    { label: 'Protein', value: 'protein' },
  ];

  constructor(
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly locationResolver: LocationResolver,
    private readonly locations: Locations,
    private readonly markets: Markets,
    private readonly productPrices: ProductPrices,
    private readonly products: Products,
    private readonly translate: TranslateService,
    private readonly userTracking: UserTracking,
  ) {}

  ngOnInit(): void {
    this.userTracking.getOrCreateTrackingGuid();
    this.priceRows = [];
    this.loadDivisions();
    this.registerMarketSearch();
    this.registerProductSearch();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get hasSelectedUnionOrWard(): boolean {
    return this.selectedUnionOrWardId !== null;
  }

  get hasPriceScope(): boolean {
    return this.hasSelectedUnionOrWard || this.selectedMarketId !== null;
  }

  get canSearchProducts(): boolean {
    return this.hasPriceScope;
  }

  get filteredRows(): PublicProductPriceResponse[] {
    return this.priceRows;
  }

  get pagedRows(): PublicProductPriceResponse[] {
    return this.filteredRows;
  }

  get totalPages(): number {
    return this.serverTotalPages;
  }

  get pageStart(): number {
    return this.totalCount === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  get emptyStateMessage(): string {
    if (!this.hasPriceScope) {
      return this.translate.instant('productPrices.states.selectLocation');
    }

    if (this.selectedProductId) {
      return this.translate.instant('productPrices.states.noProductPrices');
    }

    if (this.selectedMarketId) {
      return this.translate.instant('productPrices.states.noMarketPrices');
    }

    return this.translate.instant('productPrices.states.noUnionPrices');
  }

  setCategory(category: PriceCategory): void {
    this.selectedCategory = category;
    this.currentPage = 1;
  }

  onSearchChange(): void {
    this.currentPage = 1;
  }

  previousPage(): void {
    if (this.currentPage === 1) {
      return;
    }

    this.currentPage -= 1;
    this.loadPublicPrices();
  }

  nextPage(): void {
    if (this.currentPage >= this.totalPages) {
      return;
    }

    this.currentPage += 1;
    this.loadPublicPrices();
  }

  onDivisionChange(divisionId: string): void {
    this.selectedDivisionId = divisionId || null;
    this.resetBelowDivision();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();

    this.openLocationDropdown = null;

    if (!this.selectedDivisionId) {
      return;
    }

    this.loadDistricts(this.selectedDivisionId);
  }

  onDistrictChange(districtId: string): void {
    this.selectedDistrictId = districtId || null;
    this.resetBelowDistrict();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();

    this.openLocationDropdown = null;

    if (!this.selectedDistrictId) {
      return;
    }

    this.loadUpazilas(this.selectedDistrictId);
  }

  onUpazilaChange(upazilaId: string): void {
    this.selectedUpazilaId = upazilaId || null;
    this.resetBelowUpazila();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();

    this.openLocationDropdown = null;

    if (!this.selectedUpazilaId) {
      return;
    }

    this.loadUnionOrWards(this.selectedUpazilaId);
  }

  onUnionOrWardChange(unionOrWardId: string): void {
    this.selectedUnionOrWardId = unionOrWardId || null;
    this.clearMarketSelection();
    this.clearProductSelection();
    this.clearResults();
    this.locationStatusMessage = this.selectedUnionOrWardId
      ? this.translate.instant('productPrices.status.showingLocalPrices')
      : '';
    this.openLocationDropdown = null;

    if (this.selectedUnionOrWardId) {
      this.saveCompleteLocation();
      this.loadPublicPrices();
    } else {
      this.userTracking.clearLastKnownLocation();
    }
  }

  clearDivision(): void {
    this.openLocationDropdown = null;
    this.selectedDivisionId = null;
    this.resetBelowDivision();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  clearDistrict(): void {
    this.openLocationDropdown = null;
    this.selectedDistrictId = null;
    this.resetBelowDistrict();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  clearUpazila(): void {
    this.openLocationDropdown = null;
    this.selectedUpazilaId = null;
    this.resetBelowUpazila();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  clearUnionOrWard(): void {
    this.openLocationDropdown = null;
    this.selectedUnionOrWardId = null;
    this.clearMarketSelection();
    this.clearProductSelection();
    this.locationStatusMessage = '';
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  async useMyLocation(): Promise<void> {
    this.isRequestingGps = true;
    this.gpsStatusMessage = this.translate.instant('productPrices.gps.detecting');
    this.locationStatusMessage = '';
    this.locationErrorMessage = '';
    this.clearMarketSelection();
    this.clearProductSelection();
    this.clearResults();

    try {
      const snapshot = await this.userTracking.requestBrowserLocation();
      this.gpsStatusMessage = this.toGpsStatusMessage(snapshot);

      if (
        snapshot.gpsPermissionStatus !== 'granted' ||
        snapshot.gpsLatitude == null ||
        snapshot.gpsLongitude == null
      ) {
        return;
      }

      const approximateLocation = await firstValueFrom(
        this.locationResolver.reverseGeocode(snapshot.gpsLatitude, snapshot.gpsLongitude),
      );

      if (!approximateLocation) {
        this.locationStatusMessage = this.translate.instant('productPrices.status.detectAreaFailed');
        return;
      }

      await this.applyResolvedLocation(approximateLocation);
    } finally {
      this.isRequestingGps = false;
    }
  }

  onMarketFocus(): void {
    this.openLocationDropdown = null;
    this.isMarketDropdownOpen = true;
    this.searchMarkets(this.marketSearchText);
  }

  onMarketInputChange(value: string): void {
    this.marketSearchText = value;

    if (this.selectedMarket && value !== (this.selectedMarket.displayLabel || this.selectedMarket.marketName)) {
      this.selectedMarket = null;
      this.selectedMarketId = null;
      this.clearProductSelection();
      this.clearResults();
    }

    this.isMarketDropdownOpen = true;
    this.marketSearch$.next(value);
  }

  selectMarket(market: MarketOptionResponse): void {
    const previousLocation = this.currentLocationKey();

    this.selectedMarket = market;
    this.selectedMarketId = market.marketId;
    this.marketSearchText = market.displayLabel || market.marketName;
    this.clearProductSelection();
    this.marketOptions = [];
    this.isMarketDropdownOpen = false;
    this.marketErrorMessage = '';
    this.clearResults();
    this.applyMarketLocation(market, previousLocation);
    this.loadPublicPrices();
  }

  clearMarket(): void {
    this.clearMarketSelection();
    this.clearProductSelection();
    this.clearResults();

    if (this.hasPriceScope) {
      this.loadPublicPrices();
    }
  }

  onProductFocus(): void {
    if (!this.canSearchProducts) {
      return;
    }

    this.openLocationDropdown = null;
    this.isProductDropdownOpen = true;
    this.searchProducts(this.productSearchText);
  }

  onProductInputChange(value: string): void {
    if (!this.canSearchProducts) {
      this.clearProductSelection();
      return;
    }

    this.productSearchText = value;

    if (this.selectedProduct && value !== this.productDisplayName(this.selectedProduct)) {
      this.selectedProduct = null;
      this.selectedProductId = null;
      this.clearResults();
    }

    this.productStatusMessage = '';

    this.isProductDropdownOpen = true;
    this.productSearch$.next(value);
  }

  selectProduct(product: ProductOptionResponse): void {
    this.selectedProduct = product;
    this.selectedProductId = product.productId;
    this.productSearchText = this.productDisplayName(product);
    this.productOptions = [];
    this.isProductDropdownOpen = false;
    this.productErrorMessage = '';
    this.productStatusMessage = '';
    this.clearResults();
    this.loadPublicPrices();
  }

  clearProduct(): void {
    this.clearProductSelection();
    this.clearResults();

    if (this.hasPriceScope) {
      this.loadPublicPrices();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;

    if (!target || this.elementRef.nativeElement.contains(target)) {
      return;
    }

    this.closeDropdowns();
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.closeDropdowns();
  }

  toggleLocationDropdown(dropdown: LocationDropdown, disabled: boolean): void {
    if (disabled) {
      return;
    }

    this.openLocationDropdown = this.openLocationDropdown === dropdown ? null : dropdown;
    this.closeSearchDropdowns();
  }

  isLocationDropdownOpen(dropdown: LocationDropdown): boolean {
    return this.openLocationDropdown === dropdown;
  }

  selectDivision(divisionId: string): void {
    this.onDivisionChange(divisionId);
  }

  selectDistrict(districtId: string): void {
    this.onDistrictChange(districtId);
  }

  selectUpazila(upazilaId: string): void {
    this.onUpazilaChange(upazilaId);
  }

  selectUnionOrWard(unionOrWardId: string): void {
    this.onUnionOrWardChange(unionOrWardId);
  }

  selectedDivisionName(): string {
    return this.locationName(this.divisionOptions.find(option => option.id === this.selectedDivisionId));
  }

  selectedDistrictName(): string {
    return this.locationName(this.districtOptions.find(option => option.id === this.selectedDistrictId));
  }

  selectedUpazilaName(): string {
    return this.locationName(this.upazilaOptions.find(option => option.id === this.selectedUpazilaId));
  }

  selectedUnionOrWardName(): string {
    return this.locationName(this.unionOrWardOptions.find(option => option.id === this.selectedUnionOrWardId));
  }

  productDisplayName(product: ProductOptionResponse): string {
    if (this.currentLanguage === 'bn' && product.productNameBn) {
      return product.productNameBn;
    }

    return product.productNameEn || product.productNameBn || product.displayLabel || product.productId;
  }

  productSecondaryText(product: ProductOptionResponse): string {
    const category = this.currentLanguage === 'bn' && product.categoryNameBn
      ? product.categoryNameBn
      : product.categoryNameEn || product.categoryNameBn;
    return [category, product.primaryUnit].filter(Boolean).join(' · ');
  }

  onComboFieldMouseDown(event: MouseEvent, combo: 'market' | 'product'): void {
    const target = event.target as HTMLElement | null;

    if (!target || target.closest('.clear-field-button') || target.closest('.options-panel')) {
      return;
    }

    const field = event.currentTarget as HTMLElement;
    const fieldBounds = field.getBoundingClientRect();
    const clickedArrowArea = event.clientX >= fieldBounds.right - 44;

    if (!clickedArrowArea) {
      return;
    }

    event.preventDefault();

    if (combo === 'market') {
      this.toggleMarketDropdown();
      return;
    }

    this.toggleProductDropdown();
  }

  onComboArrowMouseDown(event: MouseEvent, combo: 'market' | 'product'): void {
    event.preventDefault();
    event.stopPropagation();

    if (combo === 'market') {
      this.toggleMarketDropdown();
      return;
    }

    this.toggleProductDropdown();
  }

  private loadDivisions(): void {
    this.isLoadingDivisions = true;
    this.locationErrorMessage = '';

    this.locations.getDivisions().subscribe({
      next: divisions => {
        this.divisionOptions = divisions;
        this.isLoadingDivisions = false;
        this.restoreSavedLocationIfPossible();
      },
      error: () => {
        this.divisionOptions = [];
        this.locationErrorMessage = this.translate.instant('productPrices.errors.loadDivisions');
        this.isLoadingDivisions = false;
      },
    });
  }

  private loadDistricts(divisionId: string): void {
    this.isLoadingDistricts = true;
    this.locationErrorMessage = '';

    this.locations.getDistricts(divisionId).subscribe({
      next: districts => {
        this.districtOptions = districts;
        this.isLoadingDistricts = false;
      },
      error: () => {
        this.districtOptions = [];
        this.locationErrorMessage = this.translate.instant('productPrices.errors.loadDistricts');
        this.isLoadingDistricts = false;
      },
    });
  }

  private loadUpazilas(districtId: string): void {
    this.isLoadingUpazilas = true;
    this.locationErrorMessage = '';

    this.locations.getUpazilas(districtId).subscribe({
      next: upazilas => {
        this.upazilaOptions = upazilas;
        this.isLoadingUpazilas = false;
      },
      error: () => {
        this.upazilaOptions = [];
        this.locationErrorMessage = this.translate.instant('productPrices.errors.loadUpazilas');
        this.isLoadingUpazilas = false;
      },
    });
  }

  private loadUnionOrWards(upazilaId: string): void {
    this.isLoadingUnionOrWards = true;
    this.locationErrorMessage = '';

    this.locations.getUnionOrWards(upazilaId).subscribe({
      next: unionOrWards => {
        this.unionOrWardOptions = unionOrWards;
        this.isLoadingUnionOrWards = false;
      },
      error: () => {
        this.unionOrWardOptions = [];
        this.locationErrorMessage = this.translate.instant('productPrices.errors.loadUnions');
        this.isLoadingUnionOrWards = false;
      },
    });
  }

  private resetBelowDivision(): void {
    this.selectedDistrictId = null;
    this.districtOptions = [];
    this.resetBelowDistrict();
  }

  private resetBelowDistrict(): void {
    this.selectedUpazilaId = null;
    this.upazilaOptions = [];
    this.resetBelowUpazila();
  }

  private resetBelowUpazila(): void {
    this.selectedUnionOrWardId = null;
    this.unionOrWardOptions = [];
    this.locationStatusMessage = '';
  }

  private clearResults(): void {
    this.priceRows = [];
    this.currentPage = 1;
    this.totalCount = 0;
    this.serverTotalPages = 1;
    this.errorMessage = '';
  }

  locationName(option: LocationResponse | undefined): string {
    if (!option) {
      return '';
    }

    return this.currentLanguage === 'bn'
      ? option.nameBn || option.nameEn || option.slug || option.id
      : option.nameEn || option.nameBn || option.slug || option.id;
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.translate.defaultLang || 'en';
  }

  private registerMarketSearch(): void {
    this.subscriptions.add(
      this.marketSearch$.pipe(
        debounceTime(250),
        switchMap(search => this.loadMarketOptions(search)),
      ).subscribe(options => {
        this.marketOptions = options;
      }),
    );
  }

  private registerProductSearch(): void {
    this.subscriptions.add(
      this.productSearch$.pipe(
        debounceTime(250),
        switchMap(search => this.loadProductOptions(search)),
      ).subscribe(options => {
        this.productOptions = options;
      }),
    );
  }

  private searchMarkets(search: string): void {
    this.marketSearch$.next(search);
  }

  private loadMarketOptions(search: string) {
    const requestScope = this.currentMarketOptionScopeKey();
    const effectiveSearch = this.effectiveMarketSearch(search);
    this.isLoadingMarkets = true;
    this.marketErrorMessage = '';

    return this.markets.getMarketOptions({
      search: effectiveSearch,
      divisionId: this.selectedDivisionId ?? undefined,
      districtId: this.selectedDistrictId ?? undefined,
      upazilaId: this.selectedUpazilaId ?? undefined,
      unionOrWardId: this.selectedUnionOrWardId ?? undefined,
      pageSize: 8,
    }).pipe(
      tap(() => {
        this.isMarketDropdownOpen = true;
      }),
      map(response => requestScope === this.currentMarketOptionScopeKey() ? response.data : []),
      catchError(() => {
        this.marketErrorMessage = this.translate.instant('productPrices.errors.loadMarkets');
        return of([]);
      }),
      finalize(() => {
        this.isLoadingMarkets = false;
      }),
    );
  }

  private searchProducts(search: string): void {
    this.productSearch$.next(search);
  }

  private loadProductOptions(search: string) {
    if (!this.canSearchProducts) {
      return of([]);
    }

    const requestScope = this.currentProductOptionScopeKey();
    this.isLoadingProducts = true;
    this.productErrorMessage = '';

    return this.products.getProductOptions({
      search: search.trim() || undefined,
      unionOrWardId: this.selectedMarketId ? undefined : this.selectedUnionOrWardId ?? undefined,
      marketId: this.selectedMarketId ?? undefined,
      pageSize: 8,
    }).pipe(
      tap(() => {
        this.isProductDropdownOpen = true;
      }),
      map(response => requestScope === this.currentProductOptionScopeKey() ? response.data : []),
      catchError(() => {
        this.productErrorMessage = this.translate.instant('productPrices.errors.loadProducts');
        return of([]);
      }),
      finalize(() => {
        this.isLoadingProducts = false;
      }),
    );
  }

  private applyMarketLocation(market: MarketOptionResponse, previousLocation: string): void {
    const marketLocation = this.marketLocationKey(market);
    const changedLocation = previousLocation !== marketLocation;

    this.selectedDivisionId = market.divisionId;
    this.selectedDistrictId = market.districtId;
    this.selectedUpazilaId = market.upazilaId;
    this.selectedUnionOrWardId = market.unionOrWardId ?? null;

    this.loadDistricts(market.divisionId);
    this.loadUpazilas(market.districtId);
    this.loadUnionOrWards(market.upazilaId);

    if (this.selectedUnionOrWardId) {
      this.saveCompleteLocation();
    } else {
      this.userTracking.clearLastKnownLocation();
    }

    if (changedLocation) {
      this.locationStatusMessage = this.translate.instant('productPrices.status.locationUpdatedForMarket');
      return;
    }

    this.locationStatusMessage = this.selectedUnionOrWardId
      ? this.translate.instant('productPrices.status.showingLocalPrices')
      : '';
  }

  private loadPublicPrices(): void {
    if (!this.hasPriceScope) {
      this.clearResults();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.productPrices.getPublicProductPrices({
      unionOrWardId: this.selectedUnionOrWardId ?? undefined,
      marketId: this.selectedMarketId ?? undefined,
      productId: this.selectedProductId ?? undefined,
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
    }).subscribe({
      next: response => {
        this.priceRows = response.data;
        this.totalCount = response.totalCount;
        this.serverTotalPages = Math.max(1, response.totalPages);
        this.isLoading = false;
      },
      error: error => {
        this.priceRows = [];
        this.totalCount = 0;
        this.serverTotalPages = 1;
        this.errorMessage = error instanceof Error ? error.message : this.translate.instant('productPrices.errors.loadPrices');
        this.isLoading = false;
      },
    });
  }

  private async restoreSavedLocationIfPossible(): Promise<void> {
    if (this.hasAttemptedSavedLocationRestore) {
      return;
    }

    this.hasAttemptedSavedLocationRestore = true;
    const savedLocation = this.userTracking.getLastKnownLocation();

    if (!savedLocation.divisionId || !savedLocation.districtId || !savedLocation.upazilaId || !savedLocation.unionOrWardId) {
      this.userTracking.clearLastKnownLocation();
      return;
    }

    try {
      if (!this.divisionOptions.some(division => division.id === savedLocation.divisionId)) {
        this.userTracking.clearLastKnownLocation();
        return;
      }

      const districts = await firstValueFrom(this.locations.getDistricts(savedLocation.divisionId));
      if (!districts.some(district => district.id === savedLocation.districtId)) {
        this.userTracking.clearLastKnownLocation();
        return;
      }

      const upazilas = await firstValueFrom(this.locations.getUpazilas(savedLocation.districtId));
      if (!upazilas.some(upazila => upazila.id === savedLocation.upazilaId)) {
        this.userTracking.clearLastKnownLocation();
        return;
      }

      const unionOrWards = await firstValueFrom(this.locations.getUnionOrWards(savedLocation.upazilaId));
      if (!unionOrWards.some(unionOrWard => unionOrWard.id === savedLocation.unionOrWardId)) {
        this.userTracking.clearLastKnownLocation();
        return;
      }

      this.selectedDivisionId = savedLocation.divisionId;
      this.selectedDistrictId = savedLocation.districtId;
      this.selectedUpazilaId = savedLocation.upazilaId;
      this.selectedUnionOrWardId = savedLocation.unionOrWardId;
      this.districtOptions = districts;
      this.upazilaOptions = upazilas;
      this.unionOrWardOptions = unionOrWards;
      this.locationStatusMessage = this.translate.instant('productPrices.status.restoredLocation');
      this.loadPublicPrices();
    } catch {
      this.userTracking.clearLastKnownLocation();
      this.locationErrorMessage = this.translate.instant('productPrices.errors.restoreLocation');
    }
  }

  private async applyResolvedLocation(location: ResolvedApproximateLocation): Promise<void> {
    const divisions = this.divisionOptions.length
      ? this.divisionOptions
      : await firstValueFrom(this.locations.getDivisions());
    this.divisionOptions = divisions;

    const matchedDivision = this.findLocationMatch(location.divisionName, divisions);
    if (!matchedDivision) {
      this.locationStatusMessage = this.translate.instant('productPrices.status.areaMatchFailed');
      return;
    }

    this.selectedDivisionId = matchedDivision.id;
    this.selectedDistrictId = null;
    this.selectedUpazilaId = null;
    this.selectedUnionOrWardId = null;
    this.districtOptions = await this.getDistrictOptions(matchedDivision.id);
    this.upazilaOptions = [];
    this.unionOrWardOptions = [];

    const matchedDistrict = this.findLocationMatch(location.districtName, this.districtOptions);
    if (!matchedDistrict) {
      this.locationStatusMessage = this.translate.instant('productPrices.status.foundDivision');
      return;
    }

    this.selectedDistrictId = matchedDistrict.id;
    this.upazilaOptions = await this.getUpazilaOptions(matchedDistrict.id);

    const matchedUpazila = this.findLocationMatch(location.upazilaName, this.upazilaOptions, true);
    if (!matchedUpazila) {
      this.locationStatusMessage = this.translate.instant('productPrices.status.foundDistrict');
      return;
    }

    this.selectedUpazilaId = matchedUpazila.id;
    this.unionOrWardOptions = await this.getUnionOrWardOptions(matchedUpazila.id);
    this.selectedUnionOrWardId = null;
    this.locationStatusMessage = this.translate.instant('productPrices.status.foundApproximateArea');
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  private async getDistrictOptions(divisionId: string): Promise<LocationResponse[]> {
    this.isLoadingDistricts = true;
    this.locationErrorMessage = '';

    try {
      return await firstValueFrom(this.locations.getDistricts(divisionId));
    } catch {
      this.locationErrorMessage = this.translate.instant('productPrices.errors.loadDistricts');
      return [];
    } finally {
      this.isLoadingDistricts = false;
    }
  }

  private async getUpazilaOptions(districtId: string): Promise<LocationResponse[]> {
    this.isLoadingUpazilas = true;
    this.locationErrorMessage = '';

    try {
      return await firstValueFrom(this.locations.getUpazilas(districtId));
    } catch {
      this.locationErrorMessage = this.translate.instant('productPrices.errors.loadUpazilas');
      return [];
    } finally {
      this.isLoadingUpazilas = false;
    }
  }

  private async getUnionOrWardOptions(upazilaId: string): Promise<LocationResponse[]> {
    this.isLoadingUnionOrWards = true;
    this.locationErrorMessage = '';

    try {
      return await firstValueFrom(this.locations.getUnionOrWards(upazilaId));
    } catch {
      this.locationErrorMessage = this.translate.instant('productPrices.errors.loadUnions');
      return [];
    } finally {
      this.isLoadingUnionOrWards = false;
    }
  }

  private findLocationMatch(
    requestedName: string | null | undefined,
    options: LocationResponse[],
    allowContains = false,
  ): LocationResponse | null {
    const normalizedRequestedName = this.normalizeLocationName(requestedName);

    if (!normalizedRequestedName) {
      return null;
    }

    const exactMatch = options.find(option =>
      this.locationNameCandidates(option).some(candidate => this.normalizeLocationName(candidate) === normalizedRequestedName),
    );

    if (exactMatch || !allowContains) {
      return exactMatch ?? null;
    }

    return options.find(option =>
      this.locationNameCandidates(option).some(candidate => {
        const normalizedCandidate = this.normalizeLocationName(candidate);
        return normalizedCandidate === normalizedRequestedName ||
          normalizedCandidate.includes(normalizedRequestedName) ||
          normalizedRequestedName.includes(normalizedCandidate);
      }),
    ) ?? null;
  }

  private locationNameCandidates(option: LocationResponse): string[] {
    return [
      option.nameEn,
      option.nameBn,
      option.slug,
    ].filter((value): value is string => Boolean(value));
  }

  private normalizeLocationName(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    return value
      .normalize('NFKC')
      .toLowerCase()
      .replace(/\bchittagong\b/g, 'chattogram')
      .replace(/\bbarisal\b/g, 'barishal')
      .replace(/\bcomilla\b/g, 'cumilla')
      .replace(/\bjessore\b/g, 'jashore')
      .replace(/\bbogra\b/g, 'bogura')
      .replace(/[-_/.,()[\]]/g, ' ')
      .replace(/\b(division|district|zila|zilla|sadar|upazila|upozila|thana|city|corporation|municipality|union|ward)\b/g, ' ')
      .replace(/(বিভাগ|জেলা|জিলা|সদর|উপজেলা|থানা|সিটি|কর্পোরেশন|পৌরসভা|ইউনিয়ন|ইউনিয়ন|ওয়ার্ড|ওয়ার্ড)/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  private saveCompleteLocation(): void {
    if (!this.selectedDivisionId || !this.selectedDistrictId || !this.selectedUpazilaId || !this.selectedUnionOrWardId) {
      this.userTracking.clearLastKnownLocation();
      return;
    }

    this.userTracking.saveLastKnownLocation({
      divisionId: this.selectedDivisionId,
      districtId: this.selectedDistrictId,
      upazilaId: this.selectedUpazilaId,
      unionOrWardId: this.selectedUnionOrWardId,
    });
  }

  private toGpsStatusMessage(snapshot: BrowserGpsSnapshot): string {
    if (snapshot.gpsPermissionStatus === 'granted') {
      return this.translate.instant('productPrices.gps.granted');
    }

    if (snapshot.gpsPermissionStatus === 'denied') {
      return this.translate.instant('productPrices.gps.denied');
    }

    if (snapshot.gpsPermissionStatus === 'error') {
      return this.translate.instant('productPrices.gps.error');
    }

    return this.translate.instant('productPrices.gps.unavailable');
  }

  private clearMarketSelection(): void {
    this.selectedMarket = null;
    this.selectedMarketId = null;
    this.marketSearchText = '';
    this.marketOptions = [];
    this.isMarketDropdownOpen = false;
    this.marketErrorMessage = '';
  }

  private clearProductSelection(): void {
    this.selectedProduct = null;
    this.selectedProductId = null;
    this.productSearchText = '';
    this.productOptions = [];
    this.isProductDropdownOpen = false;
    this.productErrorMessage = '';
    this.productStatusMessage = '';
  }

  private closeDropdowns(): void {
    this.openLocationDropdown = null;
    this.closeSearchDropdowns();
  }

  private closeSearchDropdowns(): void {
    this.isMarketDropdownOpen = false;
    this.isProductDropdownOpen = false;
  }

  private toggleMarketDropdown(): void {
    this.openLocationDropdown = null;

    if (this.isMarketDropdownOpen) {
      this.isMarketDropdownOpen = false;
      return;
    }

    this.isMarketDropdownOpen = true;
    this.searchMarkets(this.marketSearchText);
  }

  private toggleProductDropdown(): void {
    if (!this.canSearchProducts) {
      return;
    }

    this.openLocationDropdown = null;

    if (this.isProductDropdownOpen) {
      this.isProductDropdownOpen = false;
      return;
    }

    this.isProductDropdownOpen = true;
    this.searchProducts(this.productSearchText);
  }

  private currentLocationKey(): string {
    return [
      this.selectedDivisionId ?? '',
      this.selectedDistrictId ?? '',
      this.selectedUpazilaId ?? '',
      this.selectedUnionOrWardId ?? '',
    ].join('|');
  }

  private currentMarketOptionScopeKey(): string {
    return [
      this.selectedDivisionId ?? '',
      this.selectedDistrictId ?? '',
      this.selectedUpazilaId ?? '',
      this.selectedUnionOrWardId ?? '',
      this.effectiveMarketSearch(this.marketSearchText) ?? '',
    ].join('|');
  }

  private effectiveMarketSearch(search: string): string | undefined {
    const normalizedSearch = search.trim();

    if (!normalizedSearch) {
      return undefined;
    }

    if (!this.selectedMarket) {
      return normalizedSearch;
    }

    const selectedDisplayText = this.selectedMarket.displayLabel || this.selectedMarket.marketName;
    if (normalizedSearch === selectedDisplayText) {
      return undefined;
    }

    return normalizedSearch;
  }

  private currentProductOptionScopeKey(): string {
    return [
      this.selectedUnionOrWardId ?? '',
      this.selectedMarketId ?? '',
      this.productSearchText.trim(),
    ].join('|');
  }

  private marketLocationKey(market: MarketOptionResponse): string {
    return [
      market.divisionId,
      market.districtId,
      market.upazilaId,
      market.unionOrWardId ?? '',
    ].join('|');
  }
}
