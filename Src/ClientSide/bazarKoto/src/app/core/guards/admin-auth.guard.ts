import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { Auth } from '../services/auth';

export const adminAuthGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);

  if (auth.isCurrentAdmin()) {
    return true;
  }

  auth.logout();
  return router.createUrlTree(['/admin']);
};
