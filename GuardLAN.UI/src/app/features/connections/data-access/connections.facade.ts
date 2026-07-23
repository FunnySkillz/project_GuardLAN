import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { ConnectionsApi } from './connections.api';
import { ConnectionDto, ConnectionOverviewDto } from '../models/connection-overview';

export type ConnectionProtocolFilter = 'all' | 'tcp' | 'udp' | 'other';

interface ConnectionsState {
  readonly overview: ConnectionOverviewDto | null;
  readonly error: string | null;
  readonly filter: ConnectionProtocolFilter;
  readonly loading: boolean;
  readonly search: string;
}

@Injectable({ providedIn: 'root' })
export class ConnectionsFacade {
  private readonly api = inject(ConnectionsApi);
  private readonly state = signal<ConnectionsState>({
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
  readonly filteredConnections = computed(() => {
    const state = this.state();
    const query = state.search.trim().toLowerCase();
    const recentConnections = state.overview?.recentConnections ?? [];

    return recentConnections.filter((connection) => {
      if (!matchesFilter(connection, state.filter)) {
        return false;
      }

      if (!query) {
        return true;
      }

      return [
        connection.deviceName,
        connection.deviceIp,
        connection.destinationDomain,
        connection.destinationIp,
        connection.protocol,
        connection.destinationPort?.toString()
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
            error: 'Connection activity could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }

  setFilter(filter: ConnectionProtocolFilter): void {
    this.state.update((state) => ({ ...state, filter }));
  }

  setSearch(search: string): void {
    this.state.update((state) => ({ ...state, search }));
  }
}

function matchesFilter(connection: ConnectionDto, filter: ConnectionProtocolFilter): boolean {
  const protocol = connection.protocol.toLowerCase();

  switch (filter) {
    case 'tcp':
      return protocol === 'tcp';
    case 'udp':
      return protocol === 'udp';
    case 'other':
      return protocol !== 'tcp' && protocol !== 'udp';
    case 'all':
      return true;
  }
}
