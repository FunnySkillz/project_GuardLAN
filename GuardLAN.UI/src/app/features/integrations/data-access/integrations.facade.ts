import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { IntegrationsApi } from './integrations.api';
import { IntegrationHealthOverviewDto } from '../models/integration-health';

interface IntegrationsState {
  readonly data: IntegrationHealthOverviewDto | null;
  readonly error: string | null;
  readonly loading: boolean;
}

@Injectable({ providedIn: 'root' })
export class IntegrationsFacade {
  private readonly api = inject(IntegrationsApi);
  private readonly state = signal<IntegrationsState>({
    data: null,
    error: null,
    loading: false
  });

  readonly data = computed(() => this.state().data);
  readonly sources = computed(() => this.state().data?.sources ?? []);
  readonly summary = computed(() => this.state().data?.summary ?? null);
  readonly error = computed(() => this.state().error);
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
}
