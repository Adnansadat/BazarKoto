import { CommonModule } from '@angular/common';
import { Component, computed, ElementRef, HostListener, OnDestroy, OnInit, signal } from '@angular/core';
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
  selectedCategory = signal<PriceCategory>('all');
  selectedDivisionId = signal<string | null>(null);
  selectedDistrictId = signal<string | null>(null);
  selectedUpazilaId = signal<string | null>(null);
  selectedUnionOrWardId = signal<string | null>(null);
  selectedMarketId = signal<string | null>(null);
  selectedMarket = signal<MarketOptionResponse | null>(null);
  selectedProductId = signal<string | null>(null);
  selectedProduct = signal<ProductOptionResponse | null>(null);
  marketSearchText = signal('');
  productSearchText = signal('');
  currentPage = 1;
  readonly pageSize = 20;
  totalCount = 0;
  serverTotalPages = 1;
  isLoading = false;
  errorMessageKey = '';
  locationStatusMessageKey = '';
  locationErrorMessageKey = '';
  marketErrorMessageKey = '';
  productStatusMessageKey = '';
  productErrorMessageKey = '';
  gpsStatusMessageKey = '';
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
  isMarketDropdownOpen = signal(false);
  marketOptions: MarketOptionResponse[] = [];
  isLoadingProducts = false;
  isProductDropdownOpen = signal(false);
  productOptions: ProductOptionResponse[] = [];
  openLocationDropdown = signal<LocationDropdown | null>(null);
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

  readonly hasSelectedUnionOrWard = computed(() => this.selectedUnionOrWardId() !== null);
  readonly hasPriceScope = computed(() => this.hasSelectedUnionOrWard() || this.selectedMarketId() !== null);
  readonly canSearchProducts = computed(() => this.hasPriceScope());

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

  get hasErrorMessage(): boolean {
    return Boolean(this.errorMessageKey);
  }

  get emptyStateMessageKey(): string {
    if (!this.hasPriceScope()) {
      return 'productPrices.states.selectLocation';
    }

    if (this.selectedProductId()) {
      return 'productPrices.states.noProductPrices';
    }

    if (this.selectedMarketId()) {
      return 'productPrices.states.noMarketPrices';
    }

    return 'productPrices.states.noUnionPrices';
  }

  setCategory(category: PriceCategory): void {
    this.selectedCategory.set(category);
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
    this.selectedDivisionId.set(divisionId || null);
    this.resetBelowDivision();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();

    this.openLocationDropdown.set(null);

    if (!this.selectedDivisionId()) {
      return;
    }

    this.loadDistricts(this.selectedDivisionId()!);
  }

  onDistrictChange(districtId: string): void {
    this.selectedDistrictId.set(districtId || null);
    this.resetBelowDistrict();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();

    this.openLocationDropdown.set(null);

    if (!this.selectedDistrictId()) {
      return;
    }

    this.loadUpazilas(this.selectedDistrictId()!);
  }

  onUpazilaChange(upazilaId: string): void {
    this.selectedUpazilaId.set(upazilaId || null);
    this.resetBelowUpazila();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();

    this.openLocationDropdown.set(null);

    if (!this.selectedUpazilaId()) {
      return;
    }

    this.loadUnionOrWards(this.selectedUpazilaId()!);
  }

  onUnionOrWardChange(unionOrWardId: string): void {
    this.selectedUnionOrWardId.set(unionOrWardId || null);
    this.clearMarketSelection();
    this.clearProductSelection();
    this.clearResults();
    this.locationStatusMessageKey = this.selectedUnionOrWardId()
      ? 'productPrices.status.showingLocalPrices'
      : '';
    this.openLocationDropdown.set(null);

    if (this.selectedUnionOrWardId()) {
      this.saveCompleteLocation();
      this.loadPublicPrices();
    } else {
      this.userTracking.clearLastKnownLocation();
    }
  }

  clearDivision(): void {
    this.openLocationDropdown.set(null);
    this.selectedDivisionId.set(null);
    this.resetBelowDivision();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  clearDistrict(): void {
    this.openLocationDropdown.set(null);
    this.selectedDistrictId.set(null);
    this.resetBelowDistrict();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  clearUpazila(): void {
    this.openLocationDropdown.set(null);
    this.selectedUpazilaId.set(null);
    this.resetBelowUpazila();
    this.clearMarketSelection();
    this.clearProductSelection();
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  clearUnionOrWard(): void {
    this.openLocationDropdown.set(null);
    this.selectedUnionOrWardId.set(null);
    this.clearMarketSelection();
    this.clearProductSelection();
    this.locationStatusMessageKey = '';
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  async useMyLocation(): Promise<void> {
    this.isRequestingGps = true;
    this.gpsStatusMessageKey = 'productPrices.gps.detecting';
    this.locationStatusMessageKey = '';
    this.locationErrorMessageKey = '';
    this.clearMarketSelection();
    this.clearProductSelection();
    this.clearResults();

    try {
      const snapshot = await this.userTracking.requestBrowserLocation();
      this.gpsStatusMessageKey = this.toGpsStatusMessageKey(snapshot);

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
        this.locationStatusMessageKey = 'productPrices.status.detectAreaFailed';
        return;
      }

      await this.applyResolvedLocation(approximateLocation);
    } finally {
      this.isRequestingGps = false;
    }
  }

  onMarketFocus(): void {
    this.openLocationDropdown.set(null);
    this.isMarketDropdownOpen.set(true);
    this.searchMarkets(this.marketSearchText());
  }

  onMarketInputChange(value: string): void {
    this.marketSearchText.set(value);

    const selectedMarket = this.selectedMarket();
    if (selectedMarket && value !== (selectedMarket.displayLabel || selectedMarket.marketName)) {
      this.selectedMarket.set(null);
      this.selectedMarketId.set(null);
      this.clearProductSelection();
      this.clearResults();
    }

    this.isMarketDropdownOpen.set(true);
    this.marketSearch$.next(value);
  }

  selectMarket(market: MarketOptionResponse): void {
    const previousLocation = this.currentLocationKey();

    this.selectedMarket.set(market);
    this.selectedMarketId.set(market.marketId);
    this.marketSearchText.set(market.displayLabel || market.marketName);
    this.clearProductSelection();
    this.marketOptions = [];
    this.isMarketDropdownOpen.set(false);
    this.marketErrorMessageKey = '';
    this.clearResults();
    this.applyMarketLocation(market, previousLocation);
    this.loadPublicPrices();
  }

  clearMarket(): void {
    this.clearMarketSelection();
    this.clearProductSelection();
    this.clearResults();

    if (this.hasPriceScope()) {
      this.loadPublicPrices();
    }
  }

  onProductFocus(): void {
    if (!this.canSearchProducts()) {
      return;
    }

    this.openLocationDropdown.set(null);
    this.isProductDropdownOpen.set(true);
    this.searchProducts(this.productSearchText());
  }

  onProductInputChange(value: string): void {
    if (!this.canSearchProducts()) {
      this.clearProductSelection();
      return;
    }

    this.productSearchText.set(value);

    const selectedProduct = this.selectedProduct();
    if (selectedProduct && value !== this.productDisplayName(selectedProduct)) {
      this.selectedProduct.set(null);
      this.selectedProductId.set(null);
      this.clearResults();
    }

    this.productStatusMessageKey = '';

    this.isProductDropdownOpen.set(true);
    this.productSearch$.next(value);
  }

  selectProduct(product: ProductOptionResponse): void {
    this.selectedProduct.set(product);
    this.selectedProductId.set(product.productId);
    this.productSearchText.set(this.productDisplayName(product));
    this.productOptions = [];
    this.isProductDropdownOpen.set(false);
    this.productErrorMessageKey = '';
    this.productStatusMessageKey = '';
    this.clearResults();
    this.loadPublicPrices();
  }

  clearProduct(): void {
    this.clearProductSelection();
    this.clearResults();

    if (this.hasPriceScope()) {
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

    this.openLocationDropdown.update(currentDropdown => currentDropdown === dropdown ? null : dropdown);
    this.closeSearchDropdowns();
  }

  isLocationDropdownOpen(dropdown: LocationDropdown): boolean {
    return this.openLocationDropdown() === dropdown;
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
    return this.locationName(this.divisionOptions.find(option => option.id === this.selectedDivisionId()));
  }

  selectedDistrictName(): string {
    return this.locationName(this.districtOptions.find(option => option.id === this.selectedDistrictId()));
  }

  selectedUpazilaName(): string {
    return this.locationName(this.upazilaOptions.find(option => option.id === this.selectedUpazilaId()));
  }

  selectedUnionOrWardName(): string {
    return this.locationName(this.unionOrWardOptions.find(option => option.id === this.selectedUnionOrWardId()));
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
    this.locationErrorMessageKey = '';

    this.locations.getDivisions().subscribe({
      next: divisions => {
        this.divisionOptions = divisions;
        this.isLoadingDivisions = false;
        this.restoreSavedLocationIfPossible();
      },
      error: () => {
        this.divisionOptions = [];
        this.locationErrorMessageKey = 'productPrices.errors.loadDivisions';
        this.isLoadingDivisions = false;
      },
    });
  }

  private loadDistricts(divisionId: string): void {
    this.isLoadingDistricts = true;
    this.locationErrorMessageKey = '';

    this.locations.getDistricts(divisionId).subscribe({
      next: districts => {
        this.districtOptions = districts;
        this.isLoadingDistricts = false;
      },
      error: () => {
        this.districtOptions = [];
        this.locationErrorMessageKey = 'productPrices.errors.loadDistricts';
        this.isLoadingDistricts = false;
      },
    });
  }

  private loadUpazilas(districtId: string): void {
    this.isLoadingUpazilas = true;
    this.locationErrorMessageKey = '';

    this.locations.getUpazilas(districtId).subscribe({
      next: upazilas => {
        this.upazilaOptions = upazilas;
        this.isLoadingUpazilas = false;
      },
      error: () => {
        this.upazilaOptions = [];
        this.locationErrorMessageKey = 'productPrices.errors.loadUpazilas';
        this.isLoadingUpazilas = false;
      },
    });
  }

  private loadUnionOrWards(upazilaId: string): void {
    this.isLoadingUnionOrWards = true;
    this.locationErrorMessageKey = '';

    this.locations.getUnionOrWards(upazilaId).subscribe({
      next: unionOrWards => {
        this.unionOrWardOptions = unionOrWards;
        this.isLoadingUnionOrWards = false;
      },
      error: () => {
        this.unionOrWardOptions = [];
        this.locationErrorMessageKey = 'productPrices.errors.loadUnions';
        this.isLoadingUnionOrWards = false;
      },
    });
  }

  private resetBelowDivision(): void {
    this.selectedDistrictId.set(null);
    this.districtOptions = [];
    this.resetBelowDistrict();
  }

  private resetBelowDistrict(): void {
    this.selectedUpazilaId.set(null);
    this.upazilaOptions = [];
    this.resetBelowUpazila();
  }

  private resetBelowUpazila(): void {
    this.selectedUnionOrWardId.set(null);
    this.unionOrWardOptions = [];
    this.locationStatusMessageKey = '';
  }

  private clearResults(): void {
    this.priceRows = [];
    this.currentPage = 1;
    this.totalCount = 0;
    this.serverTotalPages = 1;
    this.errorMessageKey = '';
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
    this.marketErrorMessageKey = '';

    return this.markets.getMarketOptions({
      search: effectiveSearch,
      divisionId: this.selectedDivisionId() ?? undefined,
      districtId: this.selectedDistrictId() ?? undefined,
      upazilaId: this.selectedUpazilaId() ?? undefined,
      unionOrWardId: this.selectedUnionOrWardId() ?? undefined,
      pageSize: 8,
    }).pipe(
      tap(() => {
        this.isMarketDropdownOpen.set(true);
      }),
      map(response => requestScope === this.currentMarketOptionScopeKey() ? response.data : []),
      catchError(() => {
        this.marketErrorMessageKey = 'productPrices.errors.loadMarkets';
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
    if (!this.canSearchProducts()) {
      return of([]);
    }

    const requestScope = this.currentProductOptionScopeKey();
    this.isLoadingProducts = true;
    this.productErrorMessageKey = '';

    return this.products.getProductOptions({
      search: search.trim() || undefined,
      unionOrWardId: this.selectedMarketId() ? undefined : this.selectedUnionOrWardId() ?? undefined,
      marketId: this.selectedMarketId() ?? undefined,
      pageSize: 8,
    }).pipe(
      tap(() => {
        this.isProductDropdownOpen.set(true);
      }),
      map(response => requestScope === this.currentProductOptionScopeKey() ? response.data : []),
      catchError(() => {
        this.productErrorMessageKey = 'productPrices.errors.loadProducts';
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

    this.selectedDivisionId.set(market.divisionId);
    this.selectedDistrictId.set(market.districtId);
    this.selectedUpazilaId.set(market.upazilaId);
    this.selectedUnionOrWardId.set(market.unionOrWardId ?? null);

    this.loadDistricts(market.divisionId);
    this.loadUpazilas(market.districtId);
    this.loadUnionOrWards(market.upazilaId);

    if (this.selectedUnionOrWardId()) {
      this.saveCompleteLocation();
    } else {
      this.userTracking.clearLastKnownLocation();
    }

    if (changedLocation) {
      this.locationStatusMessageKey = 'productPrices.status.locationUpdatedForMarket';
      return;
    }

    this.locationStatusMessageKey = this.selectedUnionOrWardId()
      ? 'productPrices.status.showingLocalPrices'
      : '';
  }

  private loadPublicPrices(): void {
    if (!this.hasPriceScope()) {
      this.clearResults();
      return;
    }

    this.isLoading = true;
    this.errorMessageKey = '';

    this.productPrices.getPublicProductPrices({
      unionOrWardId: this.selectedUnionOrWardId() ?? undefined,
      marketId: this.selectedMarketId() ?? undefined,
      productId: this.selectedProductId() ?? undefined,
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
        this.errorMessageKey = 'productPrices.errors.loadPrices';
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

      this.selectedDivisionId.set(savedLocation.divisionId);
      this.selectedDistrictId.set(savedLocation.districtId);
      this.selectedUpazilaId.set(savedLocation.upazilaId);
      this.selectedUnionOrWardId.set(savedLocation.unionOrWardId);
      this.districtOptions = districts;
      this.upazilaOptions = upazilas;
      this.unionOrWardOptions = unionOrWards;
      this.locationStatusMessageKey = 'productPrices.status.restoredLocation';
      this.loadPublicPrices();
    } catch {
      this.userTracking.clearLastKnownLocation();
      this.locationErrorMessageKey = 'productPrices.errors.restoreLocation';
    }
  }

  private async applyResolvedLocation(location: ResolvedApproximateLocation): Promise<void> {
    const divisions = this.divisionOptions.length
      ? this.divisionOptions
      : await firstValueFrom(this.locations.getDivisions());
    this.divisionOptions = divisions;

    const matchedDivision = this.findLocationMatch(location.divisionName, divisions);
    if (!matchedDivision) {
      this.locationStatusMessageKey = 'productPrices.status.areaMatchFailed';
      return;
    }

    this.selectedDivisionId.set(matchedDivision.id);
    this.selectedDistrictId.set(null);
    this.selectedUpazilaId.set(null);
    this.selectedUnionOrWardId.set(null);
    this.districtOptions = await this.getDistrictOptions(matchedDivision.id);
    this.upazilaOptions = [];
    this.unionOrWardOptions = [];

    const matchedDistrict = this.findLocationMatch(location.districtName, this.districtOptions);
    if (!matchedDistrict) {
      this.locationStatusMessageKey = 'productPrices.status.foundDivision';
      return;
    }

    this.selectedDistrictId.set(matchedDistrict.id);
    this.upazilaOptions = await this.getUpazilaOptions(matchedDistrict.id);

    const matchedUpazila = this.findLocationMatch(location.upazilaName, this.upazilaOptions, true);
    if (!matchedUpazila) {
      this.locationStatusMessageKey = 'productPrices.status.foundDistrict';
      return;
    }

    this.selectedUpazilaId.set(matchedUpazila.id);
    this.unionOrWardOptions = await this.getUnionOrWardOptions(matchedUpazila.id);
    this.selectedUnionOrWardId.set(null);
    this.locationStatusMessageKey = 'productPrices.status.foundApproximateArea';
    this.userTracking.clearLastKnownLocation();
    this.clearResults();
  }

  private async getDistrictOptions(divisionId: string): Promise<LocationResponse[]> {
    this.isLoadingDistricts = true;
    this.locationErrorMessageKey = '';

    try {
      return await firstValueFrom(this.locations.getDistricts(divisionId));
    } catch {
      this.locationErrorMessageKey = 'productPrices.errors.loadDistricts';
      return [];
    } finally {
      this.isLoadingDistricts = false;
    }
  }

  private async getUpazilaOptions(districtId: string): Promise<LocationResponse[]> {
    this.isLoadingUpazilas = true;
    this.locationErrorMessageKey = '';

    try {
      return await firstValueFrom(this.locations.getUpazilas(districtId));
    } catch {
      this.locationErrorMessageKey = 'productPrices.errors.loadUpazilas';
      return [];
    } finally {
      this.isLoadingUpazilas = false;
    }
  }

  private async getUnionOrWardOptions(upazilaId: string): Promise<LocationResponse[]> {
    this.isLoadingUnionOrWards = true;
    this.locationErrorMessageKey = '';

    try {
      return await firstValueFrom(this.locations.getUnionOrWards(upazilaId));
    } catch {
      this.locationErrorMessageKey = 'productPrices.errors.loadUnions';
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
    if (!this.selectedDivisionId() || !this.selectedDistrictId() || !this.selectedUpazilaId() || !this.selectedUnionOrWardId()) {
      this.userTracking.clearLastKnownLocation();
      return;
    }

    this.userTracking.saveLastKnownLocation({
      divisionId: this.selectedDivisionId()!,
      districtId: this.selectedDistrictId()!,
      upazilaId: this.selectedUpazilaId()!,
      unionOrWardId: this.selectedUnionOrWardId()!,
    });
  }

  private toGpsStatusMessageKey(snapshot: BrowserGpsSnapshot): string {
    if (snapshot.gpsPermissionStatus === 'granted') {
      return 'productPrices.gps.granted';
    }

    if (snapshot.gpsPermissionStatus === 'denied') {
      return 'productPrices.gps.denied';
    }

    if (snapshot.gpsPermissionStatus === 'error') {
      return 'productPrices.gps.error';
    }

    return 'productPrices.gps.unavailable';
  }

  private clearMarketSelection(): void {
    this.selectedMarket.set(null);
    this.selectedMarketId.set(null);
    this.marketSearchText.set('');
    this.marketOptions = [];
    this.isMarketDropdownOpen.set(false);
    this.marketErrorMessageKey = '';
  }

  private clearProductSelection(): void {
    this.selectedProduct.set(null);
    this.selectedProductId.set(null);
    this.productSearchText.set('');
    this.productOptions = [];
    this.isProductDropdownOpen.set(false);
    this.productErrorMessageKey = '';
    this.productStatusMessageKey = '';
  }

  private closeDropdowns(): void {
    this.openLocationDropdown.set(null);
    this.closeSearchDropdowns();
  }

  private closeSearchDropdowns(): void {
    this.isMarketDropdownOpen.set(false);
    this.isProductDropdownOpen.set(false);
  }

  private toggleMarketDropdown(): void {
    this.openLocationDropdown.set(null);

    if (this.isMarketDropdownOpen()) {
      this.isMarketDropdownOpen.set(false);
      return;
    }

    this.isMarketDropdownOpen.set(true);
    this.searchMarkets(this.marketSearchText());
  }

  private toggleProductDropdown(): void {
    if (!this.canSearchProducts()) {
      return;
    }

    this.openLocationDropdown.set(null);

    if (this.isProductDropdownOpen()) {
      this.isProductDropdownOpen.set(false);
      return;
    }

    this.isProductDropdownOpen.set(true);
    this.searchProducts(this.productSearchText());
  }

  private currentLocationKey(): string {
    return [
      this.selectedDivisionId() ?? '',
      this.selectedDistrictId() ?? '',
      this.selectedUpazilaId() ?? '',
      this.selectedUnionOrWardId() ?? '',
    ].join('|');
  }

  private currentMarketOptionScopeKey(): string {
    return [
      this.selectedDivisionId() ?? '',
      this.selectedDistrictId() ?? '',
      this.selectedUpazilaId() ?? '',
      this.selectedUnionOrWardId() ?? '',
      this.effectiveMarketSearch(this.marketSearchText()) ?? '',
    ].join('|');
  }

  private effectiveMarketSearch(search: string): string | undefined {
    const normalizedSearch = search.trim();

    if (!normalizedSearch) {
      return undefined;
    }

    const selectedMarket = this.selectedMarket();
    if (!selectedMarket) {
      return normalizedSearch;
    }

    const selectedDisplayText = selectedMarket.displayLabel || selectedMarket.marketName;
    if (normalizedSearch === selectedDisplayText) {
      return undefined;
    }

    return normalizedSearch;
  }

  private currentProductOptionScopeKey(): string {
    return [
      this.selectedUnionOrWardId() ?? '',
      this.selectedMarketId() ?? '',
      this.productSearchText().trim(),
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
