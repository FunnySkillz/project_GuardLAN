import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { AlertsApi } from './alerts.api';
import { type AlertAction } from './alerts.facade';
import { AlertDetailDto } from '../models/alert-detail';

interface AlertDetailState {
  readonly detail: AlertDetailDto | null;
  readonly error: string | null;
  readonly loading: boolean;
  readonly noteDraft: string;
  readonly updating: boolean;
}

@Injectable({ providedIn: 'root' })
export class AlertDetailFacade {
  private readonly api = inject(AlertsApi);
  private readonly state = signal<AlertDetailState>({
    detail: null,
    error: null,
    loading: false,
    noteDraft: '',
    updating: false
  });

  readonly detail = computed(() => this.state().detail);
  readonly alert = computed(() => this.state().detail?.alert ?? null);
  readonly error = computed(() => this.state().error);
  readonly loading = computed(() => this.state().loading);
  readonly noteDraft = computed(() => this.state().noteDraft);
  readonly updating = computed(() => this.state().updating);

  load(alertId: string): void {
    this.state.update((state) => ({ ...state, error: null, loading: true }));

    this.api
      .detail(alertId)
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loading: false })))
      )
      .subscribe({
        next: (detail) => this.state.update((state) => ({ ...state, detail })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Alert details could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }

  setNote(note: string): void {
    this.state.update((state) => ({ ...state, noteDraft: note }));
  }

  submitAction(alertId: string, action: AlertAction): void {
    const request = { note: normalizeNote(this.state().noteDraft) };
    const actionRequest = (() => {
      switch (action) {
        case 'review':
          return this.api.review(alertId, request);
        case 'resolve':
          return this.api.resolve(alertId, request);
        case 'falsePositive':
          return this.api.markFalsePositive(alertId, request);
        case 'suppress':
          return this.api.suppress(alertId, request);
        case 'reopen':
          return this.api.reopen(alertId, request);
      }
    })();

    this.state.update((state) => ({ ...state, error: null, updating: true }));

    actionRequest
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, updating: false })))
      )
      .subscribe({
        next: (alert) =>
          this.state.update((state) => ({
            ...state,
            detail: state.detail ? { ...state.detail, alert } : state.detail,
            noteDraft: ''
          })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'The alert could not be updated. Check the API and try again.'
          }))
      });
  }
}

function normalizeNote(note: string): string | null {
  const value = note.trim();

  return value ? value : null;
}
