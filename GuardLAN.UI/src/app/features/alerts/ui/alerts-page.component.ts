import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { AlertAction, AlertFilter, AlertsFacade } from '../data-access/alerts.facade';
import {
  AlertDto,
  alertReviewStatusLabel,
  alertTypeLabel,
  isOpenAlert,
  severityRank
} from '../../../shared/models/security-alert';
import { LiveUpdatesService } from '../../../shared/live-updates/live-updates.service';

interface AlertFilterOption {
  readonly value: AlertFilter;
  readonly label: string;
}

@Component({
  selector: 'app-alerts-page',
  imports: [RouterLink],
  templateUrl: './alerts-page.component.html',
  styleUrl: './alerts-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlertsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly liveUpdates = inject(LiveUpdatesService);
  protected readonly facade = inject(AlertsFacade);
  protected readonly filters: readonly AlertFilterOption[] = [
    { value: 'open', label: 'Open' },
    { value: 'high', label: 'High priority' },
    { value: 'reviewed', label: 'Reviewed' },
    { value: 'resolved', label: 'Resolved' },
    { value: 'falsePositive', label: 'False positive' },
    { value: 'suppressed', label: 'Suppressed' },
    { value: 'all', label: 'All' }
  ];

  ngOnInit(): void {
    this.liveUpdates
      .ofTypes('alertUpdated', 'alertResolved', 'newAlert')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.facade.load());

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

  protected submitAction(alert: AlertDto, action: AlertAction): void {
    this.facade.submitAction(alert.id, action);
  }

  protected updateNote(alert: AlertDto, event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.facade.setNote(alert.id, target.value);
    }
  }

  protected isFilterActive(filter: AlertFilter): boolean {
    return this.facade.filter() === filter;
  }

  protected isOpen(alert: AlertDto): boolean {
    return isOpenAlert(alert);
  }

  protected isUpdating(alert: AlertDto): boolean {
    return this.facade.updatingAlertId() === alert.id;
  }

  protected alertType(alert: AlertDto): string {
    return alertTypeLabel(alert.type);
  }

  protected alertDevice(alert: AlertDto): string {
    return alert.deviceName ?? alert.deviceIpAddress ?? alert.deviceId ?? 'Network';
  }

  protected reviewStatus(alert: AlertDto): string {
    return alertReviewStatusLabel(alert.reviewStatus);
  }

  protected reviewTime(alert: AlertDto): string {
    return alert.reviewedUtc ? this.formatRelativeTime(alert.reviewedUtc) : 'Not reviewed';
  }

  protected noteDraft(alert: AlertDto): string {
    return this.facade.noteDrafts()[alert.id] ?? '';
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
