import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, map, Observable, of, take, tap } from 'rxjs';

import { LiveUpdatesService } from '../live-updates/live-updates.service';
import { AuthApi } from './auth.api';
import { AuthSessionDto } from './auth.models';

interface AuthState {
  readonly checked: boolean;
  readonly error: string | null;
  readonly loading: boolean;
  readonly loggingIn: boolean;
  readonly session: AuthSessionDto;
}

const anonymousSession: AuthSessionDto = {
  authenticated: false,
  username: null,
  expiresUtc: null
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApi);
  private readonly liveUpdates = inject(LiveUpdatesService);
  private readonly router = inject(Router);
  private readonly state = signal<AuthState>({
    checked: false,
    error: null,
    loading: false,
    loggingIn: false,
    session: anonymousSession
  });

  readonly authenticated = computed(() => this.state().session.authenticated);
  readonly checked = computed(() => this.state().checked);
  readonly error = computed(() => this.state().error);
  readonly loading = computed(() => this.state().loading);
  readonly loggingIn = computed(() => this.state().loggingIn);
  readonly session = computed(() => this.state().session);
  readonly username = computed(() => this.state().session.username);

  initialize(): Observable<boolean> {
    if (this.state().checked) {
      return of(this.authenticated());
    }

    this.state.update((state) => ({ ...state, error: null, loading: true }));

    return this.api.getSession().pipe(
      tap((session) => this.applySession(session)),
      map((session) => session.authenticated),
      catchError(() => {
        this.applySession(anonymousSession);
        return of(false);
      }),
      finalize(() => this.state.update((state) => ({ ...state, checked: true, loading: false })))
    );
  }

  login(username: string, password: string, returnUrl: string): void {
    this.state.update((state) => ({ ...state, error: null, loggingIn: true }));

    this.api
      .login({ username, password })
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loggingIn: false })))
      )
      .subscribe({
        next: (session) => {
          this.applySession(session);
          void this.router.navigateByUrl(safeReturnUrl(returnUrl));
        },
        error: () =>
          this.state.update((state) => ({
            ...state,
            session: anonymousSession,
            error: 'The username or password is incorrect.'
          }))
      });
  }

  logout(): void {
    this.api
      .logout()
      .pipe(take(1))
      .subscribe({
        next: (session) => this.applySession(session),
        error: () => {
          this.applySession(anonymousSession);
          void this.router.navigateByUrl('/login');
        },
        complete: () => void this.router.navigateByUrl('/login')
      });
  }

  private applySession(session: AuthSessionDto): void {
    this.state.update((state) => ({
      ...state,
      checked: true,
      error: null,
      session
    }));

    if (session.authenticated) {
      this.liveUpdates.connect();
    } else {
      this.liveUpdates.disconnect();
    }
  }
}

function safeReturnUrl(returnUrl: string): string {
  return returnUrl.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/';
}
