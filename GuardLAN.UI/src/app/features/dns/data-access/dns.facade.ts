import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { DnsApi } from './dns.api';
import { DnsOverviewDto, DnsQueryDto } from '../models/dns-overview';

export type DnsFilter = 'all' | 'blocked' | 'allowed';

interface DnsState {
  readonly overview: DnsOverviewDto | null;
  readonly error: string | null;
  readonly filter: DnsFilter;
  readonly loading: boolean;
  readonly search: string;
}

@Injectable({ providedIn: 'root' })
export class DnsFacade {
  private readonly api = inject(DnsApi);
  private readonly state = signal<DnsState>({
    overview: null,
    error: null,
    filter: 'all',
    loading: false,
    search: ''
  });

  readonly overview = computed(() => this.state().overview);
  readonly error = computed(() => this.state().error);
  readonly filter = computed(() => this.state().filter);
  readonly loading = computed(() => this.state().loading);
  readonly search = computed(() => this.state().search);
  readonly summary = computed(
    () =>
      this.state().overview?.summary ?? {
        totalQueries: 0,
        allowedQueries: 0,
        blockedQueries: 0,
        uniqueDomains: 0,
        activeClients: 0
      }
  );
  readonly topDomains = computed(() => this.state().overview?.topDomains ?? []);
  readonly topClients = computed(() => this.state().overview?.topClients ?? []);
  readonly filteredQueries = computed(() => {
    const state = this.state();
    const query = state.search.trim().toLowerCase();
    const recentQueries = state.overview?.recentQueries ?? [];

    return recentQueries.filter((dnsQuery) => {
      if (!matchesFilter(dnsQuery, state.filter)) {
        return false;
      }

      if (!query) {
        return true;
      }

      return [
        dnsQuery.domain,
        dnsQuery.clientIp,
        dnsQuery.deviceName
      ].some((value) => value?.toLowerCase().includes(query));
    });
  });

  load(): void {
    this.state.update((state) => ({ ...state, error: null, loading: true }));

    this.api
      .getOverview()
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loading: false })))
      )
      .subscribe({
        next: (overview) => this.state.update((state) => ({ ...state, overview })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'DNS activity could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }

  setFilter(filter: DnsFilter): void {
    this.state.update((state) => ({ ...state, filter }));
  }

  setSearch(search: string): void {
    this.state.update((state) => ({ ...state, search }));
  }
}

function matchesFilter(query: DnsQueryDto, filter: DnsFilter): boolean {
  switch (filter) {
    case 'blocked':
      return query.wasBlocked;
    case 'allowed':
      return !query.wasBlocked;
    case 'all':
      return true;
  }
}
