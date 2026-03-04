import { Injectable, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LoginGQL, RegisterGQL, GetMeGQL } from '../graphql/generated/graphql';
import { tap, map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { User, LoginInput, RegisterInput } from '../graphql/generated/graphql';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private router = inject(Router);
  private loginGQL = inject(LoginGQL);
  private registerGQL = inject(RegisterGQL);
  private getMeGQL = inject(GetMeGQL);

  private currentUserSignal = signal<User | null>(null);
  private isLoadingSignal = signal(false);
  private initializedSignal = signal(false);
  
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.currentUserSignal());
  readonly isLoading = this.isLoadingSignal.asReadonly();
  readonly isInitialized = this.initializedSignal.asReadonly();

  constructor() {
    this.loadStoredUser();
  }

  private loadStoredUser(): void {
    const token = this.getToken();
    if (token) {
      this.fetchCurrentUser().subscribe({
        complete: () => this.initializedSignal.set(true)
      });
    } else {
      this.initializedSignal.set(true);
    }
  }

  login(input: LoginInput) {
    this.isLoadingSignal.set(true);
    return this.loginGQL.mutate({ variables: { input } }).pipe(
      map(result => {
        this.isLoadingSignal.set(false);
        if (result.data) {
          const { token, user } = result.data.login;
          this.setSession(token, user);
          return user;
        }
        throw new Error('Login failed');
      }),
      catchError(error => {
        this.isLoadingSignal.set(false);
        throw error;
      })
    );
  }

  register(input: RegisterInput) {
    this.isLoadingSignal.set(true);
    return this.registerGQL.mutate({ variables: { input } }).pipe(
      map(result => {
        this.isLoadingSignal.set(false);
        if (result.data) {
          const { token, user } = result.data.register;
          this.setSession(token, user);
          return user;
        }
        throw new Error('Registration failed');
      }),
      catchError(error => {
        this.isLoadingSignal.set(false);
        throw error;
      })
    );
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    this.currentUserSignal.set(null);
    this.router.navigate(['/login']);
  }

  private setSession(token: string, user: User): void {
    localStorage.setItem('auth_token', token);
    this.currentUserSignal.set(user);
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  handleTokenExpired(): void {
    this.logout();
  }

  fetchCurrentUser() {
    return this.getMeGQL.fetch().pipe(
      map(result => {
        if (result.data?.me) {
          this.currentUserSignal.set(result.data.me);
          return result.data.me;
        }
        this.logout();
        return null;
      }),
      catchError(() => {
        this.logout();
        return of(null);
      })
    );
  }
}