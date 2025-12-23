import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (authService.isLoggedIn()) {
        return true;
    }

    // Optional: Store the attempted URL for redirecting
    return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
};
