import { CommonModule, DOCUMENT } from '@angular/common';
import { AfterViewChecked, AfterViewInit, Component, DoCheck, ElementRef, Inject, OnDestroy, OnInit, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { finalize, Subscription } from 'rxjs';
import { Api } from '../../../../core/services/api';
import { DraftService } from '../../../../core/services/draft';

interface LocationResponse {
  id: string;
  nameEn: string;
  nameBn: string;
  slug: string;
  bbsCode?: string | null;
  type?: string | null;
}

interface Market {
  id: string;
  name: string;
  area: string;
  district: string;
  divisionId: string;
  districtId: string;
  upazilaId: string;
  unionOrWardId?: string | null;
  contributors: number;
  updated: string;
}

interface MarketResponse {
  id: string;
  divisionId: string;
  divisionNameEn: string;
  divisionNameBn: string;
  districtId: string;
  districtNameEn: string;
  districtNameBn: string;
  upazilaId: string;
  upazilaNameEn: string;
  upazilaNameBn: string;
  unionOrWardId?: string | null;
  unionOrWardNameEn?: string | null;
  unionOrWardNameBn?: string | null;
  area: string;
  marketName: string;
  updatedAt?: string;
}

@Component({
  selector: 'app-markets-page',
  imports: [CommonModule, FormsModule, TranslateModule],
  standalone: true,
  templateUrl: './markets-page.component.html',
  styleUrl: './markets-page.component.scss',
})
export class MarketsPageComponent implements AfterViewInit, AfterViewChecked, OnInit, OnDestroy, DoCheck {
  @ViewChild('divisionSelect') private divisionSelect?: ElementRef<HTMLSelectElement>;
  @ViewChild('marketInput') private marketInput?: ElementRef<HTMLInputElement>;

  private readonly siteUrl = 'https://www.bazarkoto.com';
  private readonly draftStorageKey = 'bazarkoto.market.draft';
  private readonly pageUrl = `${this.siteUrl}/markets`;
  private readonly ogImageUrl = `${this.siteUrl}/images/bazar-hero.png`;
  private readonly jsonLdScriptId = 'markets-page-json-ld';
  private readonly requiredMarketFieldsMessage = 'Please complete all required market location fields.';
  private readonly duplicateMarketMessage = 'This market already exists for the selected location.';
  private readonly maxDivisionFocusRetries = 8;
  private readonly maxPostInitFocusChecks = 12;
  private lastDraftJson = '';
  private duplicateMarketFingerprint = '';
  private langChangeSubscription?: Subscription;
  private initialDivisionFocusChecks = 0;

  selectedDivisionId = signal('');
  selectedDistrictId = signal('');
  selectedUpazilaId = signal('');
  selectedUnionOrWardId = signal('');
  selectedArea = signal('');
  selectedMarketId = signal('');
  selectedMarket = signal('');
  villageOrMoholla = signal('');
  landmark = signal('');
  marketType = signal('Retail');
  operatingDays = signal('Daily');
  notes = signal('');
  showMarketValidation = signal(false);
  isLoadingMarkets = signal(true);
  isLoadingDivisions = signal(false);
  isLoadingDistricts = signal(false);
  isLoadingUpazilas = signal(false);
  isLoadingUnionOrWards = signal(false);
  isSubmittingMarket = false;
  locationErrorMessage = signal('');
  marketErrorMessage = '';
  marketSuccessMessage = '';
  divisionSearch = signal('');
  districtSearch = signal('');
  upazilaSearch = signal('');
  unionOrWardSearch = signal('');
  marketSearch = signal('');
  isDivisionInputActive = false;
  divisions = signal<LocationResponse[]>([]);
  districts = signal<LocationResponse[]>([]);
  upazilas = signal<LocationResponse[]>([]);
  unionOrWards = signal<LocationResponse[]>([]);
  nearbyMarkets = signal<Market[]>([]);

  constructor(
    private readonly router: Router,
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly translate: TranslateService,
    private readonly api: Api,
    private readonly drafts: DraftService,
    @Inject(DOCUMENT) private readonly document: Document,
  ) {}

  readonly marketTypes = [
    { value: 'Retail', labelKey: 'markets.form.marketType.retail' },
    { value: 'Wholesale', labelKey: 'markets.form.marketType.wholesale' },
    { value: 'Wet market', labelKey: 'markets.form.marketType.wetMarket' },
    { value: 'Roadside', labelKey: 'markets.form.marketType.roadside' },
    { value: 'Weekly haat', labelKey: 'markets.form.marketType.weeklyHaat' },
  ];

  readonly operatingSchedules = [
    { value: 'Daily', labelKey: 'markets.form.schedule.daily' },
    { value: 'Weekly', labelKey: 'markets.form.schedule.weekly' },
    { value: 'Morning only', labelKey: 'markets.form.schedule.morningOnly' },
    { value: 'Evening only', labelKey: 'markets.form.schedule.eveningOnly' },
    { value: 'Seasonal', labelKey: 'markets.form.schedule.seasonal' },
  ];

  get currentLanguage(): string {
    return this.translate.currentLang || this.translate.defaultLang || 'en';
  }

  get selectedDivisionName(): string {
    return this.getLocationName(this.divisions().find(item => item.id === this.selectedDivisionId()));
  }

  get selectedDistrictName(): string {
    return this.getLocationName(this.districts().find(item => item.id === this.selectedDistrictId()));
  }

  get selectedUpazilaName(): string {
    return this.getLocationName(this.upazilas().find(item => item.id === this.selectedUpazilaId()));
  }

  get selectedUnionOrWardName(): string {
    return this.getLocationName(this.unionOrWards().find(item => item.id === this.selectedUnionOrWardId()));
  }

  get matchingMarkets(): Market[] {
    return this.nearbyMarkets().filter(
      (market) =>
        market.districtId === this.selectedDistrictId() &&
        market.area === this.selectedArea(),
    );
  }

  get shouldShowDuplicateMarketError(): boolean {
    return this.marketErrorMessage === this.duplicateMarketMessage;
  }

  get selectedLocationSummary(): string {
    return this.selectedLocationSegments.join(' / ');
  }

  get selectedLocationSegments(): string[] {
    return [
      this.selectedDivisionName,
      this.selectedDistrictName,
      this.selectedUpazilaName,
      this.selectedUnionOrWardName,
      this.selectedArea().trim(),
      this.villageOrMoholla().trim(),
      this.selectedMarket().trim(),
    ].filter(Boolean);
  }

  onDivisionChange(): void {
    this.divisionSearch.set(this.selectedDivisionName);
    this.districtSearch.set('');
    this.upazilaSearch.set('');
    this.unionOrWardSearch.set('');
    this.selectedDistrictId.set('');
    this.selectedUpazilaId.set('');
    this.selectedUnionOrWardId.set('');
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.districts.set([]);
    this.upazilas.set([]);
    this.unionOrWards.set([]);
    this.loadDistricts();
    this.loadMarkets();
  }

  onDistrictChange(): void {
    this.districtSearch.set(this.selectedDistrictName);
    this.upazilaSearch.set('');
    this.unionOrWardSearch.set('');
    this.selectedUpazilaId.set('');
    this.selectedUnionOrWardId.set('');
    this.upazilas.set([]);
    this.unionOrWards.set([]);
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.loadUpazilas();
    this.loadMarkets();
  }

  onUpazilaChange(): void {
    this.upazilaSearch.set(this.selectedUpazilaName);
    this.unionOrWardSearch.set('');
    this.selectedUnionOrWardId.set('');
    this.unionOrWards.set([]);
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.loadUnionOrWards();
    this.loadMarkets();
  }

  onUnionOrWardChange(): void {
    this.unionOrWardSearch.set(this.selectedUnionOrWardName);
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.loadMarkets();
  }

  onMarketNameChange(): void {
    this.syncSelectedMarketIdFromNearbyMarkets(true);
  }

  clearDivisionSelection(): void {
    this.selectedDivisionId.set('');
    this.divisionSearch.set('');
    this.selectedDistrictId.set('');
    this.selectedUpazilaId.set('');
    this.selectedUnionOrWardId.set('');
    this.districtSearch.set('');
    this.upazilaSearch.set('');
    this.unionOrWardSearch.set('');
    this.districts.set([]);
    this.upazilas.set([]);
    this.unionOrWards.set([]);
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.loadMarkets();
  }

  clearDistrictSelection(): void {
    this.selectedDistrictId.set('');
    this.districtSearch.set('');
    this.selectedUpazilaId.set('');
    this.selectedUnionOrWardId.set('');
    this.upazilaSearch.set('');
    this.unionOrWardSearch.set('');
    this.upazilas.set([]);
    this.unionOrWards.set([]);
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.loadMarkets();
  }

  clearUpazilaSelection(): void {
    this.selectedUpazilaId.set('');
    this.upazilaSearch.set('');
    this.selectedUnionOrWardId.set('');
    this.unionOrWardSearch.set('');
    this.unionOrWards.set([]);
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.loadMarkets();
  }

  clearUnionOrWardSelection(): void {
    this.selectedUnionOrWardId.set('');
    this.unionOrWardSearch.set('');
    this.selectedMarketId.set('');
    this.selectedMarket.set('');
    this.loadMarkets();
  }

  focusMarketInput(): void {
    this.marketInput?.nativeElement.focus();
    this.marketInput?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  focusDivisionInput(): void {
    this.focusDivisionInputWithRetry();
  }

  onDivisionInputFocus(): void {
    this.isDivisionInputActive = true;
  }

  onDivisionInputBlur(): void {
    this.isDivisionInputActive = false;
  }

  selectNearbyMarket(market: Market): void {
    this.selectedDivisionId.set(market.divisionId);
    this.selectedDistrictId.set(market.districtId);
    this.selectedUpazilaId.set(market.upazilaId);
    this.selectedUnionOrWardId.set(market.unionOrWardId ?? '');
    this.divisionSearch.set(this.selectedDivisionName);
    this.districtSearch.set(market.district);
    this.upazilaSearch.set(this.selectedUpazilaName);
    this.unionOrWardSearch.set(this.selectedUnionOrWardName);
    this.selectedArea.set(market.area);
    this.selectedMarketId.set(market.id);
    this.selectedMarket.set(market.name);
    this.showMarketValidation.set(false);
    this.focusMarketInput();
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.focusDivisionInputWithRetry());
  }

  ngAfterViewChecked(): void {
    if (this.initialDivisionFocusChecks >= this.maxPostInitFocusChecks) {
      return;
    }

    this.initialDivisionFocusChecks += 1;

    const divisionElement = this.divisionSelect?.nativeElement;
    if (!divisionElement || this.isLoadingDivisions() || divisionElement.disabled) {
      return;
    }

    if (this.document.activeElement !== divisionElement) {
      this.focusDivisionInputWithRetry();
      return;
    }

    this.isDivisionInputActive = true;
    this.initialDivisionFocusChecks = this.maxPostInitFocusChecks;
  }

  ngOnInit(): void {
    this.updateSeo();
    this.restoreDraft();
    this.loadDivisions();
    this.loadDistricts();
    this.loadUpazilas();
    this.loadUnionOrWards();
    this.loadMarkets();
    this.langChangeSubscription = this.translate.onLangChange.subscribe(() => this.updateSeo());
  }

  ngOnDestroy(): void {
    this.langChangeSubscription?.unsubscribe();
    this.document.getElementById(this.jsonLdScriptId)?.remove();
  }

  ngDoCheck(): void {
    if (this.marketErrorMessage === this.requiredMarketFieldsMessage && this.isMarketFormValid()) {
      this.marketErrorMessage = '';
    }

    if (
      this.shouldShowDuplicateMarketError &&
      this.duplicateMarketFingerprint &&
      this.getDuplicateMarketFingerprint() !== this.duplicateMarketFingerprint &&
      !this.hasDuplicateMarket()
    ) {
      this.marketErrorMessage = '';
      this.duplicateMarketFingerprint = '';
    }

    this.persistDraftIfChanged();
  }

  saveDraft(): void {
    this.persistDraftIfChanged(true);
  }

  addMarket(): void {
    this.submitMarket(false);
  }

  private submitMarket(navigateWhenExisting: boolean): void {
    this.showMarketValidation.set(true);
    this.marketSuccessMessage = '';
    this.marketErrorMessage = '';
    this.duplicateMarketFingerprint = '';

    if (!this.isMarketFormValid()) {
      this.marketErrorMessage = this.requiredMarketFieldsMessage;
      return;
    }

    const duplicateMarket = this.findSelectedNearbyMarket();

    if (duplicateMarket) {
      this.selectedMarketId.set(duplicateMarket.id);
      this.marketErrorMessage = this.duplicateMarketMessage;
      this.duplicateMarketFingerprint = this.getDuplicateMarketFingerprint();
      this.persistDraftIfChanged(true);
      return;
    }

    this.isSubmittingMarket = true;
    this.api.postResponse<MarketResponse>('/Markets', {
      divisionId: this.selectedDivisionId(),
      districtId: this.selectedDistrictId(),
      upazilaId: this.selectedUpazilaId(),
      unionOrWardId: this.selectedUnionOrWardId() || null,
      area: this.selectedArea(),
      marketName: this.selectedMarket(),
      villageOrMoholla: this.villageOrMoholla(),
      landmark: this.landmark(),
      notes: this.notes().trim() || null,
      marketType: this.mapMarketType(this.marketType()),
      operatingSchedule: this.mapSchedule(this.operatingDays()),
    }).pipe(finalize(() => this.isSubmittingMarket = false)).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.marketErrorMessage = response.errors?.join(', ') || response.message || 'Unable to submit market.';
          return;
        }

        const isExistingMarket = response.message === this.duplicateMarketMessage;
        this.selectedMarketId.set(this.extractCreatedMarketId(response.data) || this.selectedMarketId());
        this.persistDraftIfChanged(true);

        if (isExistingMarket && !navigateWhenExisting) {
          this.marketErrorMessage = this.duplicateMarketMessage;
          this.duplicateMarketFingerprint = this.getDuplicateMarketFingerprint();
          return;
        }

        this.marketSuccessMessage = 'Market submitted successfully.';
        this.showMarketValidation.set(false);
        this.router.navigate(['/products']);
      },
      error: error => {
        this.marketErrorMessage = error instanceof Error ? error.message : 'Unable to submit market.';

        if (this.shouldShowDuplicateMarketError) {
          this.duplicateMarketFingerprint = this.getDuplicateMarketFingerprint();
        }
      },
    });
  }

  continueToProducts(): void {
    this.showMarketValidation.set(true);
    this.marketSuccessMessage = '';

    if (!this.isMarketFormValid()) {
      this.marketErrorMessage = this.requiredMarketFieldsMessage;
      this.duplicateMarketFingerprint = '';
      this.focusDivisionInput();
      return;
    }

    this.marketErrorMessage = '';
    this.duplicateMarketFingerprint = '';

    const duplicateMarket = this.findSelectedNearbyMarket();

    if (duplicateMarket) {
      this.selectedMarketId.set(duplicateMarket.id);
      this.persistDraftIfChanged(true);
      this.router.navigate(['/products']);
      return;
    }

    this.syncSelectedMarketIdFromNearbyMarkets(true);

    if (!this.selectedMarketId()) {
      this.submitMarket(true);
      return;
    }

    this.persistDraftIfChanged(true);
    this.router.navigate(['/products']);
  }

  loadDivisions(): void {
    this.isLoadingDivisions.set(true);
    this.locationErrorMessage.set('');

    this.api.get<LocationResponse[]>('/locations/divisions')
      .pipe(finalize(() => this.isLoadingDivisions.set(false)))
      .subscribe({
        next: divisions => {
          this.divisions.set(divisions);
          this.initialDivisionFocusChecks = 0;
          this.focusDivisionInputWithRetry();
        },
        error: error => {
          this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load divisions.');
        },
      });
  }

  loadDistricts(): void {
    const selectedDivisionId = this.selectedDivisionId();
    if (!selectedDivisionId) {
      return;
    }

    this.isLoadingDistricts.set(true);
    this.locationErrorMessage.set('');

    this.api.get<LocationResponse[]>('/locations/districts', {
      divisionId: selectedDivisionId,
    }).pipe(finalize(() => this.isLoadingDistricts.set(false))).subscribe({
      next: districts => {
        this.districts.set(districts);
      },
      error: error => {
        this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load districts.');
      },
    });
  }

  loadUpazilas(): void {
    const selectedDistrictId = this.selectedDistrictId();
    if (!selectedDistrictId) {
      return;
    }

    this.isLoadingUpazilas.set(true);
    this.locationErrorMessage.set('');

    this.api.get<LocationResponse[]>('/locations/upazilas', {
      districtId: selectedDistrictId,
    }).pipe(finalize(() => this.isLoadingUpazilas.set(false))).subscribe({
      next: upazilas => {
        this.upazilas.set(upazilas);
      },
      error: error => {
        this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load upazilas.');
      },
    });
  }

  loadUnionOrWards(): void {
    const selectedUpazilaId = this.selectedUpazilaId();
    if (!selectedUpazilaId) {
      return;
    }

    this.isLoadingUnionOrWards.set(true);
    this.locationErrorMessage.set('');

    this.api.get<LocationResponse[]>('/locations/unions-or-wards', {
      upazilaId: selectedUpazilaId,
    }).pipe(finalize(() => this.isLoadingUnionOrWards.set(false))).subscribe({
      next: unionOrWards => {
        this.unionOrWards.set(unionOrWards);
      },
      error: error => {
        this.locationErrorMessage.set(error instanceof Error ? error.message : 'Unable to load unions or wards.');
      },
    });
  }

  private loadMarkets(): void {
    this.isLoadingMarkets.set(true);

    this.api.get<MarketResponse[]>('/Markets', {
      divisionId: this.selectedDivisionId(),
      districtId: this.selectedDistrictId(),
      upazilaId: this.selectedUpazilaId(),
      unionOrWardId: this.selectedUnionOrWardId(),
      search: this.marketSearch(),
      pageNumber: 1,
      pageSize: 10,
    }).subscribe({
      next: markets => {
        this.nearbyMarkets.set(markets.slice(0, 10).map(market => ({
          id: market.id,
          name: market.marketName,
          area: market.area,
          district: this.currentLanguage === 'bn' ? market.districtNameBn : market.districtNameEn,
          divisionId: market.divisionId,
          districtId: market.districtId,
          upazilaId: market.upazilaId,
          unionOrWardId: market.unionOrWardId,
          contributors: 0,
          updated: market.updatedAt ? new Date(market.updatedAt).toLocaleDateString() : 'recently',
        })));
        this.syncSelectedMarketIdFromNearbyMarkets(false);
        this.isLoadingMarkets.set(false);
      },
      error: error => {
        this.marketErrorMessage = error instanceof Error ? error.message : 'Unable to load markets.';
        this.isLoadingMarkets.set(false);
      },
    });
  }

  getLocationName(location?: LocationResponse): string {
    if (!location) {
      return '';
    }

    return this.currentLanguage === 'bn' ? location.nameBn : location.nameEn;
  }

  private mapMarketType(value: string): string {
    return value === 'Wet market' ? 'KitchenMarket' : value === 'Roadside' ? 'TemporaryMarket' : value === 'Weekly haat' ? 'TemporaryMarket' : value;
  }

  private mapSchedule(value: string): string {
    return value === 'Morning only' ? 'Morning' : value === 'Evening only' ? 'Evening' : value;
  }

  private restoreDraft(): void {
    const draft = this.drafts.getDraft<Partial<{
      selectedDivisionId: string;
      selectedDistrictId: string;
      selectedUpazilaId: string;
      selectedUnionOrWardId: string;
      selectedArea: string;
      selectedMarketId: string;
      selectedMarket: string;
      villageOrMoholla: string;
      landmark: string;
      marketType: string;
      operatingDays: string;
      notes: string;
      divisionSearch: string;
      districtSearch: string;
      upazilaSearch: string;
      unionOrWardSearch: string;
      marketSearch: string;
    }>>(this.draftStorageKey);

    if (!draft) {
      return;
    }

    this.selectedDivisionId.set(draft.selectedDivisionId ?? '');
    this.selectedDistrictId.set(draft.selectedDistrictId ?? '');
    this.selectedUpazilaId.set(draft.selectedUpazilaId ?? '');
    this.selectedUnionOrWardId.set(draft.selectedUnionOrWardId ?? '');
    this.selectedArea.set(draft.selectedArea ?? '');
    this.selectedMarketId.set(draft.selectedMarketId ?? '');
    this.selectedMarket.set(draft.selectedMarket ?? '');
    this.villageOrMoholla.set(draft.villageOrMoholla ?? '');
    this.landmark.set(draft.landmark ?? '');
    this.marketType.set(draft.marketType ?? 'Retail');
    this.operatingDays.set(draft.operatingDays ?? 'Daily');
    this.notes.set(draft.notes ?? '');
    this.divisionSearch.set(draft.divisionSearch ?? '');
    this.districtSearch.set(draft.districtSearch ?? '');
    this.upazilaSearch.set(draft.upazilaSearch ?? '');
    this.unionOrWardSearch.set(draft.unionOrWardSearch ?? '');
    this.marketSearch.set(draft.marketSearch ?? '');
    this.lastDraftJson = JSON.stringify(this.getDraftData());
  }

  private isMarketFormValid(): boolean {
    return Boolean(
      this.selectedDivisionId() &&
      this.selectedDistrictId() &&
      this.selectedUpazilaId() &&
      this.selectedUnionOrWardId() &&
      this.selectedArea().trim() &&
      this.selectedMarket().trim() &&
      this.marketType() &&
      this.operatingDays()
    );
  }

  private hasDuplicateMarket(): boolean {
    return Boolean(this.findSelectedNearbyMarket());
  }

  private syncSelectedMarketIdFromNearbyMarkets(clearWhenMissing = false): void {
    const selectedNearbyMarket = this.findSelectedNearbyMarket();

    if (selectedNearbyMarket) {
      this.selectedMarketId.set(selectedNearbyMarket.id);
      return;
    }

    if (clearWhenMissing) {
      this.selectedMarketId.set('');
    }
  }

  private findSelectedNearbyMarket(): Market | undefined {
    const normalizedMarketName = this.normalizeComparableText(this.selectedMarket());

    return this.nearbyMarkets().find(market =>
      market.divisionId === this.selectedDivisionId() &&
      market.districtId === this.selectedDistrictId() &&
      market.upazilaId === this.selectedUpazilaId() &&
      (market.unionOrWardId ?? '') === this.selectedUnionOrWardId() &&
      this.areComparableTextsSimilar(this.normalizeComparableText(market.name), normalizedMarketName)
    );
  }

  private extractCreatedMarketId(response: unknown): string {
    if (!response || typeof response !== 'object') {
      return '';
    }

    const maybeMarket = response as Partial<MarketResponse> & { data?: Partial<MarketResponse> };
    return maybeMarket.id ?? maybeMarket.data?.id ?? '';
  }

  private normalizeComparableText(value: string): string {
    return value
      .trim()
      .toLowerCase()
      .replace(/[^\p{L}\p{N}]+/gu, '');
  }

  private areComparableTextsSimilar(left: string, right: string): boolean {
    if (!left || !right) {
      return false;
    }

    if (left === right || left.includes(right) || right.includes(left)) {
      return true;
    }

    return this.getEditDistance(left, right) <= this.getAllowedEditDistance(left, right);
  }

  private getAllowedEditDistance(left: string, right: string): number {
    const maxLength = Math.max(left.length, right.length);

    if (maxLength <= 5) {
      return 1;
    }

    if (maxLength <= 10) {
      return 2;
    }

    return 3;
  }

  private getEditDistance(left: string, right: string): number {
    const previousRow = Array.from({ length: right.length + 1 }, (_, index) => index);

    for (let leftIndex = 1; leftIndex <= left.length; leftIndex++) {
      const currentRow = [leftIndex];

      for (let rightIndex = 1; rightIndex <= right.length; rightIndex++) {
        const substitutionCost = left[leftIndex - 1] === right[rightIndex - 1] ? 0 : 1;
        currentRow[rightIndex] = Math.min(
          currentRow[rightIndex - 1] + 1,
          previousRow[rightIndex] + 1,
          previousRow[rightIndex - 1] + substitutionCost,
        );
      }

      previousRow.splice(0, previousRow.length, ...currentRow);
    }

    return previousRow[right.length];
  }

  private getDuplicateMarketFingerprint(): string {
    return [
      this.selectedDivisionId(),
      this.selectedDistrictId(),
      this.selectedUpazilaId(),
      this.selectedUnionOrWardId(),
      this.normalizeComparableText(this.selectedArea()),
      this.normalizeComparableText(this.selectedMarket()),
    ].join('|');
  }

  private persistDraftIfChanged(force = false): void {
    const draft = this.getDraftData();
    const nextDraftJson = JSON.stringify(draft);

    if (!force && nextDraftJson === this.lastDraftJson) {
      return;
    }

    this.lastDraftJson = nextDraftJson;
    this.drafts.saveDraft(this.draftStorageKey, draft);
  }

  private getDraftData(): object {
    return {
      selectedDivisionId: this.selectedDivisionId(),
      selectedDistrictId: this.selectedDistrictId(),
      selectedUpazilaId: this.selectedUpazilaId(),
      selectedUnionOrWardId: this.selectedUnionOrWardId(),
      selectedArea: this.selectedArea(),
      selectedMarketId: this.selectedMarketId(),
      selectedMarket: this.selectedMarket(),
      villageOrMoholla: this.villageOrMoholla(),
      landmark: this.landmark(),
      marketType: this.marketType(),
      operatingDays: this.operatingDays(),
      notes: this.notes(),
      divisionSearch: this.divisionSearch(),
      districtSearch: this.districtSearch(),
      upazilaSearch: this.upazilaSearch(),
      unionOrWardSearch: this.unionOrWardSearch(),
      marketSearch: this.marketSearch(),
    };
  }

  private updateSeo(): void {
    this.translate
      .get(['markets.seo.title', 'markets.seo.description', 'markets.seo.keywords'])
      .subscribe((translations) => {
        const title = translations['markets.seo.title'];
        const description = translations['markets.seo.description'];
        const keywords = translations['markets.seo.keywords'];

        this.title.setTitle(title);
        this.meta.updateTag({ name: 'description', content: description });
        this.meta.updateTag({ name: 'keywords', content: keywords });
        this.meta.updateTag({ name: 'robots', content: 'index, follow' });
        this.meta.updateTag({ property: 'og:title', content: title });
        this.meta.updateTag({ property: 'og:description', content: description });
        this.meta.updateTag({ property: 'og:type', content: 'website' });
        this.meta.updateTag({ property: 'og:url', content: this.pageUrl });
        this.meta.updateTag({ property: 'og:image', content: this.ogImageUrl });
        this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
        this.meta.updateTag({ name: 'twitter:title', content: title });
        this.meta.updateTag({ name: 'twitter:description', content: description });
        this.meta.updateTag({ name: 'twitter:image', content: this.ogImageUrl });
        this.setCanonicalUrl();
        this.setJsonLd();
      });
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
          name: this.translate.instant('markets.seo.title'),
          description: this.translate.instant('markets.seo.description'),
          inLanguage: this.translate.currentLang === 'bn' ? 'bn-BD' : 'en-BD',
          isPartOf: {
            '@id': `${this.siteUrl}/#website`,
          },
          about: [
            { '@type': 'Thing', name: 'bazar price Bangladesh' },
            { '@type': 'Thing', name: 'local market price Bangladesh' },
            { '@type': 'Thing', name: 'market price comparison' },
            { '@type': 'Thing', name: 'Bangladesh market list' },
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
              name: this.translate.instant('nav.markets'),
              item: this.pageUrl,
            },
          ],
        },
        {
          '@type': 'FAQPage',
          '@id': `${this.pageUrl}#faq`,
          mainEntity: [
            this.buildFaqSchema('markets.faq.q1.question', 'markets.faq.q1.answer'),
            this.buildFaqSchema('markets.faq.q2.question', 'markets.faq.q2.answer'),
            this.buildFaqSchema('markets.faq.q3.question', 'markets.faq.q3.answer'),
            this.buildFaqSchema('markets.faq.q4.question', 'markets.faq.q4.answer'),
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

  private focusDivisionInputWithRetry(attempt = 0): void {
    const element = this.divisionSelect?.nativeElement;

    if (!element) {
      return;
    }

    if (this.isLoadingDivisions() || element.disabled) {
      if (attempt < this.maxDivisionFocusRetries) {
        setTimeout(() => this.focusDivisionInputWithRetry(attempt + 1), 80);
      }
      return;
    }

    element.focus();
    this.isDivisionInputActive = this.document.activeElement === element;
  }
}
