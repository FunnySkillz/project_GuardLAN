import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { DashboardApi } from './dashboard.api';
import { DashboardOverviewDto, NetworkScanDto } from '../models/dashboard-overview';

interface DashboardState {
  readonly data: DashboardOverviewDto | null;
  readonly error: string | null;
  readonly loading: boolean;
  readonly queueingScan: boolean;
}

@Injectable({ providedIn: 'root' })
export class DashboardFacade {
  private readonly api = inject(DashboardApi);
  private readonly state = signal<DashboardState>({
    data: null,
    error: null,
    loading: false,
    queueingScan: false
  });

  readonly data = computed(() => this.state().data);
  readonly devices = computed(() => this.state().data?.devices ?? []);
  readonly alerts = computed(() => this.state().data?.summary.recentAlerts ?? []);
  readonly domains = computed(() => this.state().data?.summary.mostContactedExternalDomains ?? []);
  readonly traffic = computed(
    () =>
      this.state().data?.summary.connectionTraffic ?? {
        totalConnections: 0,
        activeDevices: 0,
        uniqueDestinations: 0,
        bytesSent: 0,
        bytesReceived: 0
      }
  );
  readonly protocols = computed(() => this.state().data?.summary.topConnectionProtocols ?? []);
  readonly scans = computed(() => this.state().data?.recentScans ?? []);
  readonly error = computed(() => this.state().error);
  readonly loading = computed(() => this.state().loading);
  readonly queueingScan = computed(() => this.state().queueingScan);

  load(): void {
    this.state.update((state) => ({ ...state, error: null, loading: true }));

    this.api
      .getOverview()
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loading: false })))
      )
      .subscribe({
        next: (data) => this.state.update((state) => ({ ...state, data })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'GuardLAN API is unavailable. Start the backend and database, then retry.'
          }))
      });
  }

  queueScan(subnet: string): void {
    this.state.update((state) => ({ ...state, error: null, queueingScan: true }));

    this.api
      .queueScan({ subnet })
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, queueingScan: false })))
      )
      .subscribe({
        next: (scan) =>
          this.state.update((state) => ({
            ...state,
            data: addScanToOverview(state.data, scan)
          })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'The scan could not be queued. Check that the GuardLAN API is running.'
          }))
      });
  }
}

function addScanToOverview(
  overview: DashboardOverviewDto | null,
  scan: NetworkScanDto
): DashboardOverviewDto | null {
  if (!overview) {
    return overview;
  }

  const recentScans = [
    scan,
    ...overview.recentScans.filter((recentScan) => recentScan.id !== scan.id)
  ].slice(0, 10);

  return {
    ...overview,
    recentScans
  };
}
