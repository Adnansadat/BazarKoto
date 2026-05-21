import { Routes } from '@angular/router';

export const LIVE_PRICES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/live-prices-page/live-prices-page.component')
        .then(m => m.LivePricesPageComponent)
  }
];
