import { Routes } from '@angular/router';

export const PRODUCT_PRICES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/product-prices-page/product-prices-page.component')
        .then(m => m.ProductPricesPageComponent)
  }
];
