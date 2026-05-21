import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full'
  },

  {
    path: 'home',
    loadChildren: () =>
      import('./modules/home/home.routes')
        .then(m => m.HOME_ROUTES)
  },

  {
    path: 'products',
    loadChildren: () =>
      import('./modules/products/products.routes')
        .then(m => m.PRODUCTS_ROUTES)
  },

  {
    path: 'markets',
    loadChildren: () =>
      import('./modules/markets/markets.routes')
        .then(m => m.MARKETS_ROUTES)
  },

  {
    path: 'prices',
    loadChildren: () =>
      import('./modules/prices/prices.routes')
        .then(m => m.PRICES_ROUTES)
  },

  {
    path: 'live-prices',
    loadChildren: () =>
      import('./modules/live-prices/live-prices.routes')
        .then(m => m.LIVE_PRICES_ROUTES)
  },

  {
    path: 'admin',
    loadChildren: () =>
      import('./modules/admin/admin.routes')
        .then(m => m.ADMIN_ROUTES)
  },

  {
    path: 'about',
    loadComponent: () =>
      import('./modules/about/about-page.component')
        .then(m => m.AboutPageComponent)
  },

  {
    path: 'contact',
    loadComponent: () =>
      import('./modules/contact/contact-page.component')
        .then(m => m.ContactPageComponent)
  },

  {
    path: 'privacy-policy',
    loadComponent: () =>
      import('./modules/privacy/privacy-policy-page.component')
        .then(m => m.PrivacyPolicyPageComponent)
  }
];
