import { Routes } from '@angular/router';
import { adminAuthGuard } from '../../core/guards/admin-auth.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/admin-login/admin-login.component')
        .then(m => m.AdminLoginComponent)
  },
  {
    path: 'dashboard',
    canActivate: [adminAuthGuard],
    loadComponent: () =>
      import('./pages/admin-dashboard/admin-dashboard.component')
        .then(m => m.AdminDashboardComponent)
  },
  {
    path: 'messages',
    canActivate: [adminAuthGuard],
    loadComponent: () =>
      import('./pages/admin-contact-messages/admin-contact-messages.component')
        .then(m => m.AdminContactMessagesComponent)
  },
  {
    path: 'contact-messages',
    redirectTo: 'messages',
    pathMatch: 'full'
  }
];
