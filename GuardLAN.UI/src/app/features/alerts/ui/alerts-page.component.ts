import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';

import { AlertFilter, AlertsFacade } from '../data-access/alerts.facade';
import {
  AlertDto,
  alertTypeLabel,
  isOpenAlert,
  severityRank
} from '../../../shared/models/security-alert';

interface AlertFilterOption {
  readonly value: AlertFilter;
  readonly label: string;
}

@Component({
  selector: 'app-alerts-page',
  templateUrl: './alerts-page.component.html',
  styleUrl: './alerts-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlertsPageComponent implements OnInit {
  protected readonly facade = inject(AlertsFacade);
  protected readonly filters: readonly AlertFilterOption[] = [
    { value: 'open', label: 'Open' },
    { value: 'high', label: 'High priority' },
    { value: 'resolved', label: 'Resolved' },
    { value: 'all', label: 'All' }
  ];

  ngOnInit(): void {
    this.facade.load();
  }

  protected retry(): void {
    this.facade.load();
  }

  protected setFilter(filter: AlertFilter): void {
    this.facade.setFilter(filter);
  }

  protected updateSearch(event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.facade.setSearch(target.value);
    }
  }

  protected resolve(alert: AlertDto): void {
    this.facade.resolve(alert.id);
  }

  protected isFilterActive(filter: AlertFilter): boolean {
    return this.facade.filter() === filter;
  }

  protected isOpen(alert: AlertDto): boolean {
    return isOpenAlert(alert);
  }

  protected isResolving(alert: AlertDto): boolean {
    return this.facade.resolvingAlertId() === alert.id;
  }

  protected alertType(alert: AlertDto): string {
    return alertTypeLabel(alert.type);
  }

  protected alertDevice(alert: AlertDto): string {
    return alert.deviceName ?? alert.deviceIpAddress ?? alert.deviceId ?? 'Network';
  }

  protected priorityLabel(alert: AlertDto): string {
    return severityRank(alert.severity) >= 3 ? 'Priority' : 'Watch';
  }

  protected formatRelativeTime(value: string | null): string {
    if (!value) {
      return 'Not resolved';
    }

    const timestamp = new Date(value).getTime();

    if (Number.isNaN(timestamp)) {
      return 'Unknown';
    }

    const elapsedSeconds = Math.max(0, Math.floor((Date.now() - timestamp) / 1000));

    if (elapsedSeconds < 60) {
      return 'just now';
    }

    const elapsedMinutes = Math.floor(elapsedSeconds / 60);

    if (elapsedMinutes < 60) {
      return `${elapsedMinutes} min ago`;
    }

    const elapsedHours = Math.floor(elapsedMinutes / 60);

    if (elapsedHours < 24) {
      return `${elapsedHours} hr ago`;
    }

    const elapsedDays = Math.floor(elapsedHours / 24);

    return `${elapsedDays} day${elapsedDays === 1 ? '' : 's'} ago`;
  }
}
