import { CommonModule, DOCUMENT } from '@angular/common';
import { AfterViewChecked, AfterViewInit, ChangeDetectionStrategy, Component, DoCheck, ElementRef, Inject, OnDestroy, OnInit, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { finalize, Subscription } from 'rxjs';
import { Api } from '../../../../core/services/api';
import { DraftService } from '../../../../core/services/draft';

interface ProductCategory {
  id: string;
  nameEn: string;
  nameBn: string;
  slug: string;
  descriptionEn?: string | null;
  descriptionBn?: string | null;
  sortOrder: number;
  isActive: boolean;
}

interface Product {
  id: string;
  name: string;
  categoryId: string;
  category: string;
  unit: string;
  freshness: string;
  submissions: number;
}

interface ProductResponse {
  id: string;
  categoryId: string;
  categoryNameEn: string;
  categoryNameBn: string;
  nameEn: string;
  nameBn: string;
  localName?: string | null;
  slug: string;
  primaryUnit: string;
  productState: string;
  notes?: string | null;
  status: string;
  isActive: boolean;
}

interface ProductCategoryResponse {
  id: string;
  nameEn: string;
  nameBn: string;
  slug: string;
  descriptionEn?: string | null;
  descriptionBn?: string | null;
  sortOrder: number;
  isActive: boolean;
}

interface CreateProductRequest {
  categoryId: string;
  nameEn: string;
  nameBn: string;
  localName: string | null;
  primaryUnit: string;
  productState: string;
  notes: string | null;
}

interface SelectOption {
  value: string;
  labelKey: string;
}

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
  templateUrl: './products-page.component.html',
  styleUrl: './products-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductsPageComponent implements AfterViewInit, AfterViewChecked, OnInit, OnDestroy, DoCheck {
  @ViewChild('categoryInput') private categoryInput?: ElementRef<HTMLSelectElement>;
  @ViewChild('productNameInput') private productNameInput?: ElementRef<HTMLSelectElement>;

  private readonly draftStorageKey = 'bazarkoto.product.draft';
  private readonly requiredProductFieldsMessage = 'Please complete all required product fields.';
  private readonly duplicateProductMessage = 'This product already exists in the selected category.';
  private readonly maxCategoryFocusRetries = 8;
  private readonly maxPostInitFocusChecks = 12;
  private readonly siteUrl = 'https://www.bazarkoto.com';
  private readonly pageUrl = `${this.siteUrl}/products`;
  private readonly ogImageUrl = `${this.siteUrl}/images/bazar-hero.png`;
  private readonly jsonLdScriptId = 'products-page-json-ld';
  private lastDraftJson = '';
  private duplicateProductFingerprint = '';
  private langChangeSubscription?: Subscription;
  private initialCategoryFocusChecks = 0;

  selectedCategoryId = signal('');
  categorySearch = signal('');
  productName = signal('');
  localName = signal('');
  selectedUnit = signal('kg');
  selectedState = signal('Fresh');
  selectedProductId = signal('');
  notes = signal('');
  searchTerm = signal('');
  showProductValidation = signal(false);
  isLoadingProducts = signal(true);
  isLoadingCategories = signal(true);
  isSubmittingProduct = signal(false);
  isCategoryInputActive = signal(false);
  categoryErrorMessage = signal('');
  productErrorMessage = signal('');
  productListErrorMessage = signal('');
  productSuccessMessage = signal('');
  products = signal<Product[]>([]);
  productSuggestions = signal<ProductResponse[]>([]);

  constructor(
    private readonly router: Router,
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly translate: TranslateService,
    private readonly api: Api,
    private readonly drafts: DraftService,
    @Inject(DOCUMENT) private readonly document: Document,
  ) {}

  categories = signal<ProductCategory[]>([]);

  readonly units: SelectOption[] = [
    { value: 'kg', labelKey: 'products.unit.kg' },
    { value: 'gram', labelKey: 'products.unit.gram' },
    { value: 'piece', labelKey: 'products.unit.piece' },
    { value: 'dozen', labelKey: 'products.unit.dozen' },
    { value: 'litre', labelKey: 'products.unit.litre' },
    { value: 'packet', labelKey: 'products.unit.packet' },
    { value: 'bundle', labelKey: 'products.unit.bundle' },
    { value: 'hali', labelKey: 'products.unit.hali' },
  ];

  readonly states: SelectOption[] = [
    { value: 'Fresh', labelKey: 'products.state.fresh' },
    { value: 'Dry', labelKey: 'products.state.dry' },
    { value: 'Frozen', labelKey: 'products.state.frozen' },
    { value: 'Processed', labelKey: 'products.state.processed' },
    { value: 'Packaged', labelKey: 'products.state.packaged' },
  ];

  get visibleProducts(): Product[] {
    return this.products();
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.translate.defaultLang || 'en';
  }

  get selectedCategoryName(): string {
    return this.getCategoryName(this.categories().find(category => category.id === this.selectedCategoryId()));
  }

  get selectedProductSegments(): string[] {
    return [
      this.selectedCategoryName,
      this.productName().trim(),
      this.selectedUnit(),
      this.selectedState(),
    ].filter(Boolean);
  }

  get categoryInvalid(): boolean {
    return this.showProductValidation() && !this.selectedCategoryId();
  }

  get productNameInvalid(): boolean {
    return this.showProductValidation() && !this.productName().trim();
  }

  get productNameErrorKey(): string {
    return 'products.form.nameRequired';
  }

  get localNameInvalid(): boolean {
    return this.showProductValidation() && !this.localName().trim();
  }

  get unitInvalid(): boolean {
    return this.showProductValidation() && !this.selectedUnit();
  }

  get stateInvalid(): boolean {
    return this.showProductValidation() && !this.selectedState();
  }

  get hasDuplicateSelection(): boolean {
    return Boolean(this.getDuplicateProduct());
  }

  getProductOptionLabel(product: ProductResponse): string {
    const mainName = this.getProductName(product);
    const alternateName = this.currentLanguage === 'bn' ? product.nameEn : product.nameBn;

    return alternateName && alternateName !== mainName ? `${mainName} (${alternateName})` : mainName;
  }

  ngOnInit(): void {
    this.updateSeo();
    this.restoreDraft();
    this.loadCategories();
    this.langChangeSubscription = this.translate.onLangChange.subscribe(() => {
      this.updateSeo();
      this.remapProductsForLanguage();
    });
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.focusCategoryInputWithRetry());
  }

  ngAfterViewChecked(): void {
    if (this.initialCategoryFocusChecks >= this.maxPostInitFocusChecks) {
      return;
    }

    this.initialCategoryFocusChecks += 1;

    const element = this.categoryInput?.nativeElement;
    if (!element || this.isLoadingCategories() || element.disabled) {
      return;
    }

    if (this.document.activeElement !== element) {
      this.focusCategoryInputWithRetry();
      return;
    }

    this.isCategoryInputActive.set(true);
    this.initialCategoryFocusChecks = this.maxPostInitFocusChecks;
  }

  ngOnDestroy(): void {
    this.langChangeSubscription?.unsubscribe();
    this.document.getElementById(this.jsonLdScriptId)?.remove();
  }

  ngDoCheck(): void {
    if (this.productErrorMessage() === this.requiredProductFieldsMessage && this.isProductFormValid()) {
      this.productErrorMessage.set('');
    }

    if (
      this.productErrorMessage() === this.duplicateProductMessage &&
      this.duplicateProductFingerprint &&
      this.getDuplicateProductFingerprint() !== this.duplicateProductFingerprint &&
      !this.hasDuplicateProduct()
    ) {
      this.productErrorMessage.set('');
      this.duplicateProductFingerprint = '';
    }

    this.persistDraftIfChanged();
  }

  focusProductNameInput(): void {
    this.productNameInput?.nativeElement.focus();
    this.productNameInput?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  addProduct(): void {
    this.showProductValidation.set(true);
    this.productErrorMessage.set('');
    this.productSuccessMessage.set('');
    this.duplicateProductFingerprint = '';

    const duplicateProduct = this.getDuplicateProduct();

    if (duplicateProduct) {
      this.productErrorMessage.set(this.duplicateProductMessage);
      this.duplicateProductFingerprint = this.getDuplicateProductFingerprint();
      this.selectedProductId.set(duplicateProduct.id);
      this.productNameInput?.nativeElement.focus();
      return;
    }

    if (!this.isProductFormValid()) {
      this.productErrorMessage.set(this.requiredProductFieldsMessage);
      this.productNameInput?.nativeElement.focus();
      return;
    }

    this.isSubmittingProduct.set(true);

    this.api.post<ProductResponse>('/Products', this.getCreateProductPayload())
      .pipe(finalize(() => this.isSubmittingProduct.set(false)))
      .subscribe({
        next: product => {
          this.storeSelectedProduct(product);
          this.productSuccessMessage.set('Product submitted successfully.');
          this.showProductValidation.set(false);
          this.clearDraft(false);
          this.loadProducts();
          this.router.navigate(['/prices']);
        },
        error: error => {
          this.productErrorMessage.set(error instanceof Error ? error.message : 'Unable to submit product.');

          if (this.productErrorMessage() === this.duplicateProductMessage) {
            this.duplicateProductFingerprint = this.getDuplicateProductFingerprint();
          }
        },
      });
  }

  onCategoryChange(): void {
    this.categorySearch.set(this.selectedCategoryName);
    this.selectedProductId.set('');
    this.productName.set('');
    this.localName.set('');
    this.productSuggestions.set([]);
    this.clearProductValidationIfReady();
    this.loadProducts();
    this.loadProductSuggestions();
  }

  onCategoryInputChange(): void {
    this.onCategoryChange();
  }

  onProductSelectionChange(): void {
    const selectedProduct = this.getSelectedProduct();

    if (selectedProduct) {
      this.productName.set(selectedProduct.nameEn);
      this.localName.set(selectedProduct.localName || selectedProduct.nameBn || '');
      this.selectedUnit.set(selectedProduct.primaryUnit);
      this.selectedState.set(selectedProduct.productState);
      this.notes.set(selectedProduct.notes || this.notes());
    } else {
      this.productName.set('');
    }

    this.productSuccessMessage.set('');
    this.clearProductValidationIfReady();
  }

  onRequiredFieldChange(): void {
    this.clearProductValidationIfReady();
  }

  continueToPrices(): void {
    this.showProductValidation.set(true);
    this.productSuccessMessage.set('');

    if (!this.isProductFormValid()) {
      this.productErrorMessage.set(this.requiredProductFieldsMessage);
      this.duplicateProductFingerprint = '';
      this.focusCategoryInputWithRetry();
      return;
    }

    const duplicateProduct = this.getDuplicateProduct();

    if (duplicateProduct) {
      this.productErrorMessage.set(this.duplicateProductMessage);
      this.duplicateProductFingerprint = this.getDuplicateProductFingerprint();
      this.selectedProductId.set(duplicateProduct.id);
      this.storeSelectedProduct(duplicateProduct);
      this.router.navigate(['/prices']);
      return;
    }

    this.addProduct();
  }

  clearCategorySelection(): void {
    this.selectedCategoryId.set('');
    this.categorySearch.set('');
    this.productSuggestions.set([]);
    this.clearProductSelection();
    this.loadProducts();
    this.clearProductValidationIfReady();
  }

  clearProductSelection(): void {
    this.selectedProductId.set('');
    this.productName.set('');
    this.productSuccessMessage.set('');
    this.clearProductValidationIfReady();
  }

  saveDraft(): void {
    this.persistDraftIfChanged(true);
    this.productErrorMessage.set('');
  }

  clearDraft(showMessage = true): void {
    this.drafts.clearDraft(this.draftStorageKey);

    if (showMessage) {
      this.productSuccessMessage.set('');
    }
  }

  loadProducts(): void {
    this.isLoadingProducts.set(true);
    this.productListErrorMessage.set('');

    this.api.get<ProductResponse[]>('/Products', {
      categoryId: this.selectedCategoryId(),
      search: this.searchTerm(),
      pageNumber: 1,
      pageSize: 18,
    }).pipe(finalize(() => this.isLoadingProducts.set(false))).subscribe({
      next: products => {
        this.products.set(this.mapProducts(products.slice(0, 18)));
      },
      error: error => {
        this.productListErrorMessage.set(error instanceof Error ? error.message : 'Unable to load products.');
      },
    });
  }

  private loadCategories(): void {
    this.isLoadingCategories.set(true);
    this.categoryErrorMessage.set('');

    this.api.get<ProductCategoryResponse[]>('/product-categories')
      .pipe(finalize(() => this.isLoadingCategories.set(false)))
      .subscribe({
        next: categories => {
          this.categories.set(categories.map(category => ({
            id: category.id,
            nameEn: category.nameEn,
            nameBn: category.nameBn,
            slug: category.slug,
            descriptionEn: category.descriptionEn,
            descriptionBn: category.descriptionBn,
            sortOrder: category.sortOrder,
            isActive: category.isActive,
          })));
          if (this.selectedCategoryId()) {
            this.categorySearch.set(this.selectedCategoryName);
          }
          this.initialCategoryFocusChecks = 0;
          this.focusCategoryInputWithRetry();
          this.loadProducts();
          this.loadProductSuggestions();
        },
        error: error => {
          this.categoryErrorMessage.set(error instanceof Error ? error.message : 'Unable to load product categories.');
          this.loadProducts();
        },
      });
  }

  getCategoryName(category?: ProductCategory): string {
    if (!category) {
      return '';
    }

    return this.currentLanguage === 'bn' ? category.nameBn : category.nameEn;
  }

  private loadProductSuggestions(): void {
    if (!this.selectedCategoryId()) {
      this.productSuggestions.set([]);
      return;
    }

    this.api.get<ProductResponse[]>('/Products', {
      categoryId: this.selectedCategoryId(),
      pageNumber: 1,
      pageSize: 100,
    }).subscribe({
      next: products => {
        this.productSuggestions.set(products);
        this.selectedProductId.set(this.selectedProductId() || (this.findProductByName(products, this.productName())?.id ?? ''));

        if (this.selectedProductId()) {
          this.onProductSelectionChange();
        }

        this.clearProductValidationIfReady();
      },
      error: () => {
        this.productSuggestions.set([]);
      },
    });
  }

  private restoreDraft(): void {
    const draft = this.drafts.getDraft<Partial<{
      selectedCategoryId: string;
      categorySearch: string;
      productName: string;
      localName: string;
      selectedUnit: string;
      selectedState: string;
      selectedProductId: string;
      notes: string;
    }>>(this.draftStorageKey);

    if (!draft) {
      return;
    }

    this.selectedCategoryId.set(draft.selectedCategoryId ?? '');
    this.categorySearch.set(draft.categorySearch ?? '');
    this.productName.set(draft.productName ?? '');
    this.localName.set(draft.localName ?? '');
    this.selectedUnit.set(draft.selectedUnit ?? 'kg');
    this.selectedState.set(draft.selectedState ?? 'Fresh');
    this.selectedProductId.set(draft.selectedProductId ?? '');
    this.notes.set(draft.notes ?? '');
    this.lastDraftJson = JSON.stringify(this.getDraftData());
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
      selectedCategoryId: this.selectedCategoryId(),
      categorySearch: this.categorySearch(),
      productName: this.productName(),
      localName: this.localName(),
      selectedUnit: this.selectedUnit(),
      selectedState: this.selectedState(),
      selectedProductId: this.selectedProductId(),
      notes: this.notes(),
    };
  }

  private getSelectedProduct(): ProductResponse | undefined {
    return this.productSuggestions().find(product => product.id === this.selectedProductId())
      ?? this.findProductByName(this.productSuggestions(), this.productName());
  }

  private clearProductValidationIfReady(): void {
    if (!this.showProductValidation()) {
      return;
    }

    if (this.isProductFormValid() && !this.hasDuplicateProduct()) {
      this.showProductValidation.set(false);
      this.productErrorMessage.set('');
    }
  }

  private findProductByName(products: ProductResponse[], value: string): ProductResponse | undefined {
    const normalizedValue = value.trim().toLowerCase();

    if (!normalizedValue) {
      return undefined;
    }

    return products.find(product =>
      product.nameEn.toLowerCase() === normalizedValue ||
      product.nameBn.toLowerCase() === normalizedValue ||
      (product.localName?.toLowerCase() === normalizedValue)
    );
  }

  private mapProducts(products: ProductResponse[]): Product[] {
    return products.map(product => ({
      id: product.id,
      name: this.currentLanguage === 'bn' ? product.nameBn : product.nameEn,
      categoryId: product.categoryId,
      category: this.currentLanguage === 'bn' ? product.categoryNameBn : product.categoryNameEn,
      unit: product.primaryUnit,
      freshness: product.productState,
      submissions: 0,
    }));
  }

  private getProductName(product: ProductResponse): string {
    return this.currentLanguage === 'bn' ? product.nameBn : product.nameEn;
  }

  private isProductFormValid(): boolean {
    return Boolean(
      this.selectedCategoryId() &&
      this.productName().trim() &&
      this.localName().trim() &&
      this.selectedUnit() &&
      this.selectedState()
    );
  }

  private getCreateProductPayload(): CreateProductRequest {
    const localOrAlternateName = this.localName().trim();

    return {
      categoryId: this.selectedCategoryId(),
      nameEn: this.productName().trim(),
      nameBn: localOrAlternateName,
      localName: localOrAlternateName,
      primaryUnit: this.selectedUnit(),
      productState: this.selectedState(),
      notes: this.notes().trim() || null,
    };
  }

  private hasDuplicateProduct(): boolean {
    return Boolean(this.getDuplicateProduct());
  }

  private getDuplicateProduct(): ProductResponse | undefined {
    const slug = this.slugify(this.productName());

    if (!slug) {
      return undefined;
    }

    return this.productSuggestions().find(product =>
      product.categoryId === this.selectedCategoryId() &&
      this.slugify(product.nameEn || product.slug) === slug
    );
  }

  private getDuplicateProductFingerprint(): string {
    return `${this.selectedCategoryId()}|${this.slugify(this.productName())}`;
  }

  private slugify(value: string): string {
    return value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  private storeSelectedProduct(product: ProductResponse): void {
    localStorage.setItem('bazarKoto.selectedProduct', JSON.stringify({
      id: product.id,
      categoryId: product.categoryId,
      categoryNameEn: product.categoryNameEn,
      categoryNameBn: product.categoryNameBn,
      nameEn: product.nameEn,
      nameBn: product.nameBn,
      localName: product.localName,
      primaryUnit: product.primaryUnit,
      productState: product.productState,
      notes: product.notes,
      status: product.status,
      isActive: product.isActive,
    }));
  }

  private remapProductsForLanguage(): void {
    this.loadProducts();
  }

  private updateSeo(): void {
    this.translate
      .get([
        'products.seo.title',
        'products.seo.description',
        'products.seo.keywords',
        'products.seo.ogTitle',
        'products.seo.ogDescription',
      ])
      .subscribe((translations) => {
        const title = translations['products.seo.title'];
        const description = translations['products.seo.description'];
        const keywords = translations['products.seo.keywords'];
        const ogTitle = translations['products.seo.ogTitle'];
        const ogDescription = translations['products.seo.ogDescription'];

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
          name: this.translate.instant('products.seo.title'),
          description: this.translate.instant('products.seo.description'),
          inLanguage: this.translate.currentLang === 'bn' ? 'bn-BD' : 'en-BD',
          isPartOf: {
            '@id': `${this.siteUrl}/#website`,
          },
          about: [
            { '@type': 'Thing', name: 'product price Bangladesh' },
            { '@type': 'Thing', name: 'grocery price Bangladesh' },
            { '@type': 'Thing', name: 'bazar product list' },
            { '@type': 'Thing', name: 'market price comparison' },
            { '@type': 'Thing', name: 'submit product price' },
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
              name: this.translate.instant('nav.products'),
              item: this.pageUrl,
            },
          ],
        },
        {
          '@type': 'FAQPage',
          '@id': `${this.pageUrl}#faq`,
          mainEntity: [
            this.buildFaqSchema('products.faq.q1.question', 'products.faq.q1.answer'),
            this.buildFaqSchema('products.faq.q2.question', 'products.faq.q2.answer'),
            this.buildFaqSchema('products.faq.q3.question', 'products.faq.q3.answer'),
            this.buildFaqSchema('products.faq.q4.question', 'products.faq.q4.answer'),
          ],
        },
        {
          '@type': 'ItemList',
          '@id': `${this.pageUrl}#product-categories`,
          name: this.translate.instant('products.schema.categoryListName'),
          itemListElement: this.categories().map((category, index) => ({
            '@type': 'ListItem',
            position: index + 1,
            name: this.getCategoryName(category),
            description: this.currentLanguage === 'bn' ? category.descriptionBn : category.descriptionEn,
          })),
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

  onCategoryInputFocus(): void {
    this.isCategoryInputActive.set(true);
  }

  onCategoryInputBlur(): void {
    this.isCategoryInputActive.set(false);
  }

  private focusCategoryInputWithRetry(attempt = 0): void {
    const element = this.categoryInput?.nativeElement;

    if (!element) {
      return;
    }

    if (this.isLoadingCategories() || element.disabled) {
      if (attempt < this.maxCategoryFocusRetries) {
        setTimeout(() => this.focusCategoryInputWithRetry(attempt + 1), 80);
      }
      return;
    }

    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    element.focus();
    this.isCategoryInputActive.set(this.document.activeElement === element);
  }
}
