import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { IntegrationsApi } from './integrations.api';
import {
  IntegrationHealthOverviewDto,
  IntegrationImportTarget
} from '../models/integration-health';

interface IntegrationsState {
  readonly data: IntegrationHealthOverviewDto | null;
  readonly error: string | null;
  readonly importingTarget: IntegrationImportTarget | null;
  readonly loading: boolean;
}

@Injectable({ providedIn: 'root' })
export class IntegrationsFacade {
  private readonly api = inject(IntegrationsApi);
  private readonly state = signal<IntegrationsState>({
    data: null,
    error: null,
    importingTarget: null,
    loading: false
  });

  readonly data = computed(() => this.state().data);
  readonly sources = computed(() => this.state().data?.sources ?? []);
  readonly recentRuns = computed(() => this.state().data?.recentRuns ?? []);
  readonly summary = computed(() => this.state().data?.summary ?? null);
  readonly error = computed(() => this.state().error);
  readonly importingTarget = computed(() => this.state().importingTarget);
  readonly loading = computed(() => this.state().loading);

  load(): void {
    this.state.update((state) => ({ ...state, error: null, loading: true }));

    this.api
      .health()
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loading: false })))
      )
      .subscribe({
        next: (data) => this.state.update((state) => ({ ...state, data })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Integration health could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }

  importNow(target: IntegrationImportTarget): void {
    this.state.update((state) => ({ ...state, error: null, importingTarget: target }));

    this.api
      .importNow(target)
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, importingTarget: null })))
      )
      .subscribe({
        next: () => this.load(),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Import could not be started. Check the source configuration and API logs.'
          }))
      });
  }
}
