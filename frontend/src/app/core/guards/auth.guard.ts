import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If not initialized, allow navigation (will show loading state)
  if (!authService.isInitialized()) {
    return true;
  }

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

export const publicGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If not initialized, check if there's a token (user was logged in)
  if (!authService.isInitialized()) {
    if (authService.getToken()) {
      // Has token but not initialized - redirect to chats
      return router.createUrlTree(['/chats']);
    }
    // No token - allow navigation to login
    return true;
  }

  if (!authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/chats']);
};