import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { AlertsApi } from './alerts.api';
import { AlertDto, isOpenAlert, severityRank } from '../../../shared/models/security-alert';

export type AlertFilter = 'open' | 'high' | 'resolved' | 'all';

interface AlertsState {
  readonly alerts: readonly AlertDto[];
  readonly error: string | null;
  readonly filter: AlertFilter;
  readonly loading: boolean;
  readonly resolvingAlertId: string | null;
  readonly search: string;
}

@Injectable({ providedIn: 'root' })
export class AlertsFacade {
  private readonly api = inject(AlertsApi);
  private readonly state = signal<AlertsState>({
    alerts: [],
    error: null,
    filter: 'open',
    loading: false,
    resolvingAlertId: null,
    search: ''
  });

  readonly alerts = computed(() => this.state().alerts);
  readonly error = computed(() => this.state().error);
  readonly filter = computed(() => this.state().filter);
  readonly loading = computed(() => this.state().loading);
  readonly resolvingAlertId = computed(() => this.state().resolvingAlertId);
  readonly search = computed(() => this.state().search);
  readonly filteredAlerts = computed(() => {
    const state = this.state();
    const query = state.search.trim().toLowerCase();

    return state.alerts.filter((alert) => {
      if (!matchesFilter(alert, state.filter)) {
        return false;
      }

      if (!query) {
        return true;
      }

      return [
        alert.type,
        alert.message,
        alert.severity,
        alert.deviceName,
        alert.deviceIpAddress,
        alert.deviceMacAddress
      ].some((value) => value?.toLowerCase().includes(query));
    });
  });
  readonly summary = computed(() => {
    const alerts = this.state().alerts;

    return {
      total: alerts.length,
      open: alerts.filter(isOpenAlert).length,
      high: alerts.filter((alert) => isOpenAlert(alert) && severityRank(alert.severity) >= 3).length,
      resolved: alerts.filter((alert) => !isOpenAlert(alert)).length
    };
  });

  load(): void {
    this.state.update((state) => ({ ...state, error: null, loading: true }));

    this.api
      .list()
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loading: false })))
      )
      .subscribe({
        next: (alerts) => this.state.update((state) => ({ ...state, alerts })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Alerts could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }

  setFilter(filter: AlertFilter): void {
    this.state.update((state) => ({ ...state, filter }));
  }

  setSearch(search: string): void {
    this.state.update((state) => ({ ...state, search }));
  }

  resolve(alertId: string): void {
    this.state.update((state) => ({ ...state, error: null, resolvingAlertId: alertId }));

    this.api
      .resolve(alertId)
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, resolvingAlertId: null })))
      )
      .subscribe({
        next: (resolvedAlert) =>
          this.state.update((state) => ({
            ...state,
            alerts: state.alerts.map((alert) =>
              alert.id === resolvedAlert.id ? resolvedAlert : alert
            )
          })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'The alert could not be resolved. Check the API and try again.'
          }))
      });
  }
}

function matchesFilter(alert: AlertDto, filter: AlertFilter): boolean {
  switch (filter) {
    case 'open':
      return isOpenAlert(alert);
    case 'high':
      return isOpenAlert(alert) && severityRank(alert.severity) >= 3;
    case 'resolved':
      return !isOpenAlert(alert);
    case 'all':
      return true;
  }
}
