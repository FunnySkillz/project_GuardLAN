import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { AlertsApi } from './alerts.api';
import {
  AlertDto,
  alertReviewStatusLabel,
  isOpenAlert,
  severityRank
} from '../../../shared/models/security-alert';

export type AlertFilter =
  | 'open'
  | 'high'
  | 'reviewed'
  | 'resolved'
  | 'falsePositive'
  | 'suppressed'
  | 'all';
export type AlertAction = 'review' | 'resolve' | 'falsePositive' | 'suppress' | 'reopen';

interface AlertsState {
  readonly alerts: readonly AlertDto[];
  readonly error: string | null;
  readonly filter: AlertFilter;
  readonly loading: boolean;
  readonly noteDrafts: Readonly<Record<string, string>>;
  readonly search: string;
  readonly updatingAlertId: string | null;
}

@Injectable({ providedIn: 'root' })
export class AlertsFacade {
  private readonly api = inject(AlertsApi);
  private readonly state = signal<AlertsState>({
    alerts: [],
    error: null,
    filter: 'open',
    loading: false,
    noteDrafts: {},
    search: '',
    updatingAlertId: null
  });

  readonly alerts = computed(() => this.state().alerts);
  readonly error = computed(() => this.state().error);
  readonly filter = computed(() => this.state().filter);
  readonly loading = computed(() => this.state().loading);
  readonly noteDrafts = computed(() => this.state().noteDrafts);
  readonly search = computed(() => this.state().search);
  readonly updatingAlertId = computed(() => this.state().updatingAlertId);
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
        alertReviewStatusLabel(alert.reviewStatus),
        alert.reviewNote,
        alert.deviceName,
        alert.deviceIpAddress,
        alert.deviceMacAddress,
        alert.source,
        alert.sourceRecordId,
        alert.sourceIp,
        alert.destinationIp,
        alert.protocol,
        alert.evidenceSummary
      ].some((value) => value?.toLowerCase().includes(query));
    });
  });
  readonly summary = computed(() => {
    const alerts = this.state().alerts;

    return {
      total: alerts.length,
      open: alerts.filter(isOpenAlert).length,
      high: alerts.filter((alert) => isOpenAlert(alert) && severityRank(alert.severity) >= 3).length,
      reviewed: alerts.filter((alert) => alert.reviewStatus === 'Reviewed').length,
      falsePositive: alerts.filter((alert) => alert.reviewStatus === 'FalsePositive').length,
      suppressed: alerts.filter((alert) => alert.reviewStatus === 'Suppressed').length,
      closed: alerts.filter((alert) => !isOpenAlert(alert)).length
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

  setNote(alertId: string, note: string): void {
    this.state.update((state) => ({
      ...state,
      noteDrafts: {
        ...state.noteDrafts,
        [alertId]: note
      }
    }));
  }

  submitAction(alertId: string, action: AlertAction): void {
    const request = { note: normalizeNote(this.state().noteDrafts[alertId]) };
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

    this.state.update((state) => ({ ...state, error: null, updatingAlertId: alertId }));

    actionRequest
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, updatingAlertId: null })))
      )
      .subscribe({
        next: (updatedAlert) =>
          this.state.update((state) => ({
            ...state,
            noteDrafts: removeDraft(state.noteDrafts, alertId),
            alerts: state.alerts.map((alert) =>
              alert.id === updatedAlert.id ? updatedAlert : alert
            )
          })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'The alert could not be updated. Check the API and try again.'
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
    case 'reviewed':
      return alert.reviewStatus === 'Reviewed';
    case 'resolved':
      return alert.reviewStatus === 'Resolved';
    case 'falsePositive':
      return alert.reviewStatus === 'FalsePositive';
    case 'suppressed':
      return alert.reviewStatus === 'Suppressed';
    case 'all':
      return true;
  }
}

function normalizeNote(note: string | undefined): string | null {
  const value = note?.trim();

  return value ? value : null;
}

function removeDraft(
  drafts: Readonly<Record<string, string>>,
  alertId: string
): Readonly<Record<string, string>> {
  const next = { ...drafts };
  delete next[alertId];

  return next;
}
