import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { ConnectionsApi, defaultConnectionOverviewQuery } from './connections.api';
import {
  ConnectionOverviewDto,
  ConnectionOverviewQuery,
  ConnectionPageDto,
  ConnectionProtocolFilter
} from '../models/connection-overview';

interface ConnectionsState {
  readonly overview: ConnectionOverviewDto | null;
  readonly error: string | null;
  readonly loading: boolean;
  readonly query: ConnectionOverviewQuery;
}

@Injectable({ providedIn: 'root' })
export class ConnectionsFacade {
  private readonly api = inject(ConnectionsApi);
  private readonly state = signal<ConnectionsState>({
    overview: null,
    error: null,
    loading: false,
    query: defaultConnectionOverviewQuery
  });

  readonly overview = computed(() => this.state().overview);
  readonly error = computed(() => this.state().error);
  readonly filter = computed(() => this.state().query.protocol);
  readonly loading = computed(() => this.state().loading);
  readonly query = computed(() => this.state().query);
  readonly search = computed(() => this.state().query.search);
  readonly pageSize = computed(() => this.state().query.pageSize);
  readonly summary = computed(
    () =>
      this.state().overview?.summary ?? {
        totalConnections: 0,
        activeDevices: 0,
        uniqueDestinations: 0,
        totalBytesSent: 0,
        totalBytesReceived: 0
      }
  );
  readonly topProtocols = computed(() => this.state().overview?.topProtocols ?? []);
  readonly topDestinations = computed(() => this.state().overview?.topDestinations ?? []);
  readonly topDevices = computed(() => this.state().overview?.topDevices ?? []);
  readonly pagination = computed(() => this.state().overview?.recentConnections ?? emptyConnectionPage);
  readonly connections = computed(() => this.pagination().items);
  readonly hasPreviousPage = computed(() => this.pagination().page > 1);
  readonly hasNextPage = computed(() => {
    const pagination = this.pagination();

    return pagination.totalPages > 0 && pagination.page < pagination.totalPages;
  });

  load(): void {
    this.state.update((state) => ({ ...state, error: null, loading: true }));
    const query = this.state().query;

    this.api
      .getOverview(query)
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loading: false })))
      )
      .subscribe({
        next: (overview) => this.state.update((state) => ({ ...state, overview })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Connection activity could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }

  setFilter(filter: ConnectionProtocolFilter): void {
    this.updateQuery({ protocol: filter, page: 1 });
    this.load();
  }

  setSearch(search: string): void {
    this.updateQuery({ search });
  }

  applySearch(): void {
    this.updateQuery({ page: 1 });
    this.load();
  }

  goToPreviousPage(): void {
    if (!this.hasPreviousPage()) {
      return;
    }

    this.updateQuery({ page: this.pagination().page - 1 });
    this.load();
  }

  goToNextPage(): void {
    if (!this.hasNextPage()) {
      return;
    }

    this.updateQuery({ page: this.pagination().page + 1 });
    this.load();
  }

  setPageSize(pageSize: number): void {
    if (!Number.isFinite(pageSize) || pageSize <= 0) {
      return;
    }

    this.updateQuery({ page: 1, pageSize });
    this.load();
  }

  private updateQuery(query: Partial<ConnectionOverviewQuery>): void {
    this.state.update((state) => ({
      ...state,
      query: {
        ...state.query,
        ...query
      }
    }));
  }
}

const emptyConnectionPage: ConnectionPageDto = {
  items: [],
  page: 1,
  pageSize: defaultConnectionOverviewQuery.pageSize,
  totalCount: 0,
  totalPages: 0
};
