import { Routes } from '@angular/router';

export const MARKETS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/markets-page/markets-page.component')
        .then(m => m.MarketsPageComponent)
  }
];