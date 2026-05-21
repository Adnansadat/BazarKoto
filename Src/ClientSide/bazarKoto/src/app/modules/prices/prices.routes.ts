import { Routes } from '@angular/router';

export const PRICES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/prices-page/prices-page.component')
        .then(m => m.PricesPageComponent)
  }
];