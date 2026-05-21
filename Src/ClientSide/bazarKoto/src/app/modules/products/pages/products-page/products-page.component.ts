import { CommonModule, DOCUMENT } from '@angular/common';
import { AfterViewInit, Component, DoCheck, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
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

interface CreateProductRequest {
  categoryId: string;
  nameEn: string;
  nameBn: string;
  localName: string | null;
  primaryUnit: string;
  productState: string;
  notes: string | null;
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
})
export class ProductsPageComponent implements AfterViewInit, OnInit, OnDestroy, DoCheck {
  @ViewChild('productNameInput') private productNameInput?: ElementRef<HTMLInputElement>;

  private readonly draftStorageKey = 'bazarkoto.product.draft';
  private readonly siteUrl = 'https://www.bazarkoto.com';
  private readonly pageUrl = `${this.siteUrl}/products`;
  private readonly ogImageUrl = `${this.siteUrl}/images/bazar-hero.png`;
  private readonly jsonLdScriptId = 'products-page-json-ld';
  private lastDraftJson = '';
  private langChangeSubscription?: Subscription;

  selectedCategoryId = '';
  categorySearch = '';
  productName = '';
  localName = '';
  selectedUnit = 'kg';
  selectedState = 'Fresh';
  selectedProductId = '';
  notes = '';
  searchTerm = '';
  showProductValidation = false;
  isLoadingProducts = true;
  isLoadingCategories = true;
  isSubmittingProduct = false;
  categoryErrorMessage = '';
  productErrorMessage = '';
  productSuccessMessage = '';
  products: Product[] = [];
  productSuggestions: ProductResponse[] = [];

  constructor(
    private readonly router: Router,
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly translate: TranslateService,
    private readonly api: Api,
    private readonly drafts: DraftService,
    @Inject(DOCUMENT) private readonly document: Document,
  ) {}

  categories: ProductCategory[] = [];

  readonly units: SelectOption[] = [
    { value: 'kg', labelKey: 'products.unit.kg' },
    { value: 'gram', labelKey: 'products.unit.gram' },
    { value: 'piece', labelKey: 'products.unit.piece' },
    { value: 'dozen', labelKey: 'products.unit.dozen' },
    { value: 'litre', labelKey: 'products.unit.litre' },
    { value: 'packet', labelKey: 'products.unit.packet' },
  ];

  readonly states: SelectOption[] = [
    { value: 'Fresh', labelKey: 'products.state.fresh' },
    { value: 'Dry', labelKey: 'products.state.dry' },
    { value: 'Frozen', labelKey: 'products.state.frozen' },
    { value: 'Processed', labelKey: 'products.state.processed' },
  ];

  get visibleProducts(): Product[] {
    return this.products;
  }

  get currentLanguage(): string {
    return this.translate.currentLang || this.translate.defaultLang || 'en';
  }

  get selectedCategoryName(): string {
    return this.getCategoryName(this.categories.find(category => category.id === this.selectedCategoryId));
  }

  get categoryInvalid(): boolean {
    return this.showProductValidation && !this.selectedCategoryId;
  }

  get productNameInvalid(): boolean {
    return this.showProductValidation && (!this.productName.trim() || !this.getSelectedProduct());
  }

  get productNameErrorKey(): string {
    return this.productName.trim() ? 'products.form.nameSelectionRequired' : 'products.form.nameRequired';
  }

  get unitInvalid(): boolean {
    return this.showProductValidation && !this.selectedUnit;
  }

  get stateInvalid(): boolean {
    return this.showProductValidation && !this.selectedState;
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
    setTimeout(() => this.productNameInput?.nativeElement.focus());
  }

  ngOnDestroy(): void {
    this.langChangeSubscription?.unsubscribe();
    this.document.getElementById(this.jsonLdScriptId)?.remove();
  }

  ngDoCheck(): void {
    this.persistDraftIfChanged();
  }

  focusProductNameInput(): void {
    this.productNameInput?.nativeElement.focus();
    this.productNameInput?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  addProduct(): void {
    this.showProductValidation = true;
    this.productErrorMessage = '';
    this.productSuccessMessage = '';
    this.syncSelectedCategoryFromSearch();
    this.syncSelectedProductFromSearch();

    const selectedProduct = this.getSelectedProduct();

    if (!this.selectedCategoryId || !selectedProduct || !this.selectedUnit || !this.selectedState) {
      this.productErrorMessage = 'Please complete all required product fields.';
      this.productNameInput?.nativeElement.focus();
      return;
    }

    localStorage.setItem('bazarKoto.selectedProduct', JSON.stringify({
      id: selectedProduct.id,
      categoryId: selectedProduct.categoryId,
      nameEn: selectedProduct.nameEn,
      nameBn: selectedProduct.nameBn,
      localName: selectedProduct.localName,
      primaryUnit: selectedProduct.primaryUnit,
      productState: selectedProduct.productState,
    }));
    this.productSuccessMessage = 'Product selected successfully.';
    this.router.navigate(['/prices']);
  }

  onCategoryChange(): void {
    this.syncSelectedCategoryFromSearch();
    this.selectedProductId = '';
    this.productName = '';
    this.localName = '';
    this.productSuggestions = [];
    this.clearProductValidationIfReady();
    this.loadProducts();
    this.loadProductSuggestions();
  }

  onCategoryInputChange(): void {
    this.syncSelectedCategoryFromSearch();
    this.onCategoryChange();
  }

  onProductNameInputChange(): void {
    this.selectedProductId = '';
    this.loadProductSuggestions();
    this.syncSelectedProductFromSearch();
    this.clearProductValidationIfReady();
  }

  onRequiredFieldChange(): void {
    this.clearProductValidationIfReady();
  }

  private applySelectedProduct(existingProduct: ProductResponse | undefined): void {
    if (!existingProduct) {
      return;
    }

    this.selectedProductId = existingProduct.id;
    this.localName = existingProduct.localName || this.localName;
    this.selectedUnit = existingProduct.primaryUnit || this.selectedUnit;
    this.selectedState = existingProduct.productState || this.selectedState;
  }

  saveDraft(): void {
    this.syncSelectedCategoryFromSearch();
    this.persistDraftIfChanged(true);
    this.productErrorMessage = '';
  }

  clearDraft(showMessage = true): void {
    this.drafts.clearDraft(this.draftStorageKey);

    if (showMessage) {
      this.productSuccessMessage = '';
    }
  }

  loadProducts(): void {
    this.isLoadingProducts = true;

    this.api.get<ProductResponse[]>('/Products', {
      categoryId: this.selectedCategoryId,
      search: this.searchTerm,
      pageNumber: 1,
      pageSize: 20,
    }).pipe(finalize(() => this.isLoadingProducts = false)).subscribe({
      next: products => {
        this.products = this.mapProducts(products);
      },
      error: error => {
        this.productErrorMessage = error instanceof Error ? error.message : 'Unable to load products.';
      },
    });
  }

  private loadCategories(): void {
    this.isLoadingCategories = true;
    this.categoryErrorMessage = '';

    this.api.get<ProductCategoryResponse[]>('/product-categories')
      .pipe(finalize(() => this.isLoadingCategories = false))
      .subscribe({
        next: categories => {
          this.categories = categories.map(category => ({
            id: category.id,
            nameEn: category.nameEn,
            nameBn: category.nameBn,
            slug: category.slug,
            descriptionEn: category.descriptionEn,
            descriptionBn: category.descriptionBn,
            sortOrder: category.sortOrder,
            isActive: category.isActive,
          }));
          if (this.selectedCategoryId) {
            this.categorySearch = this.selectedCategoryName;
          }
          this.loadProducts();
          this.loadProductSuggestions();
        },
        error: error => {
          this.categoryErrorMessage = error instanceof Error ? error.message : 'Unable to load product categories.';
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
    this.api.get<ProductResponse[]>('/Products', {
      categoryId: this.selectedCategoryId,
      search: this.productName,
      pageNumber: 1,
      pageSize: 20,
    }).subscribe({
      next: products => {
        this.productSuggestions = products;
        this.syncSelectedProductFromSearch();
        this.clearProductValidationIfReady();
      },
      error: () => {
        this.productSuggestions = [];
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

    this.selectedCategoryId = draft.selectedCategoryId ?? '';
    this.categorySearch = draft.categorySearch ?? '';
    this.productName = draft.productName ?? '';
    this.localName = draft.localName ?? '';
    this.selectedUnit = draft.selectedUnit ?? 'kg';
    this.selectedState = draft.selectedState ?? 'Fresh';
    this.selectedProductId = draft.selectedProductId ?? '';
    this.notes = draft.notes ?? '';
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
      selectedCategoryId: this.selectedCategoryId,
      categorySearch: this.categorySearch,
      productName: this.productName,
      localName: this.localName,
      selectedUnit: this.selectedUnit,
      selectedState: this.selectedState,
      selectedProductId: this.selectedProductId,
      notes: this.notes,
    };
  }

  private syncSelectedCategoryFromSearch(): void {
    const match = this.findCategoryByName(this.categorySearch);
    this.selectedCategoryId = match?.id ?? '';
  }

  private syncSelectedProductFromSearch(): void {
    const match = this.findProductByName(this.productSuggestions, this.productName);
    this.applySelectedProduct(match);
  }

  private getSelectedProduct(): ProductResponse | undefined {
    return this.productSuggestions.find(product => product.id === this.selectedProductId)
      ?? this.findProductByName(this.productSuggestions, this.productName);
  }

  private clearProductValidationIfReady(): void {
    if (!this.showProductValidation) {
      return;
    }

    this.syncSelectedCategoryFromSearch();
    this.syncSelectedProductFromSearch();

    if (this.selectedCategoryId && this.getSelectedProduct() && this.selectedUnit && this.selectedState) {
      this.showProductValidation = false;
      this.productErrorMessage = '';
    }
  }

  private findCategoryByName(value: string): ProductCategory | undefined {
    const normalizedValue = value.trim().toLowerCase();

    if (!normalizedValue) {
      return undefined;
    }

    return this.categories.find(category =>
      category.nameEn.toLowerCase() === normalizedValue ||
      category.nameBn.toLowerCase() === normalizedValue
    );
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
          itemListElement: this.categories.map((category, index) => ({
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
}
