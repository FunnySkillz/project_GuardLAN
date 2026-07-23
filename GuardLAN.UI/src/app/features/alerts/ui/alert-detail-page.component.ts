import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AlertDetailFacade } from '../data-access/alert-detail.facade';
import { type AlertAction } from '../data-access/alerts.facade';
import { ConnectionDto } from '../../connections/models/connection-overview';
import {
  AlertDto,
  alertReviewStatusLabel,
  alertTypeLabel,
  isOpenAlert,
  severityRank
} from '../../../shared/models/security-alert';
import { LiveUpdatesService } from '../../../shared/live-updates/live-updates.service';

@Component({
  selector: 'app-alert-detail-page',
  imports: [RouterLink],
  templateUrl: './alert-detail-page.component.html',
  styleUrl: './alert-detail-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlertDetailPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly liveUpdates = inject(LiveUpdatesService);
  private readonly route = inject(ActivatedRoute);
  protected readonly facade = inject(AlertDetailFacade);
  private alertId = '';

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.alertId = params.get('id') ?? '';

      if (this.alertId) {
        this.facade.load(this.alertId);
      }
    });

    this.liveUpdates
      .ofTypes('alertUpdated', 'alertResolved', 'newAlert')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((update) => {
        if (update.alertId === this.alertId) {
          this.retry();
        }
      });
  }

  protected retry(): void {
    if (this.alertId) {
      this.facade.load(this.alertId);
    }
  }

  protected pageTitle(): string {
    const alert = this.facade.alert();

    return alert ? this.alertType(alert) : 'Alert detail';
  }

  protected submitAction(action: AlertAction): void {
    if (this.alertId) {
      this.facade.submitAction(this.alertId, action);
    }
  }

  protected updateNote(event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.facade.setNote(target.value);
    }
  }

  protected isOpen(alert: AlertDto): boolean {
    return isOpenAlert(alert);
  }

  protected isPriority(alert: AlertDto): boolean {
    return severityRank(alert.severity) >= 3;
  }

  protected alertType(alert: AlertDto): string {
    return alertTypeLabel(alert.type);
  }

  protected reviewStatus(alert: AlertDto): string {
    return alertReviewStatusLabel(alert.reviewStatus);
  }

  protected alertDevice(alert: AlertDto): string {
    return alert.deviceName ?? alert.deviceIpAddress ?? alert.deviceId ?? 'Network';
  }

  protected alertSource(alert: AlertDto): string {
    return alert.source ?? 'GuardLAN';
  }

  protected sourceEndpoint(alert: AlertDto): string {
    return alert.sourceIp ?? alert.deviceIpAddress ?? 'Unknown source';
  }

  protected destinationEndpoint(alert: AlertDto): string {
    const destination = alert.destinationIp ?? 'Unknown destination';
    const port = alert.destinationPort === null ? '' : `:${alert.destinationPort}`;
    const protocol = alert.protocol ? ` ${alert.protocol.toUpperCase()}` : '';

    return `${destination}${port}${protocol}`;
  }

  protected connectionDestination(connection: ConnectionDto): string {
    return connection.destinationDomain ?? connection.destinationIp;
  }

  protected connectionEndpoint(connection: ConnectionDto): string {
    return connection.destinationPort === null
      ? connection.protocol.toUpperCase()
      : `${connection.protocol.toUpperCase()}/${connection.destinationPort}`;
  }

  protected totalTraffic(connection: ConnectionDto): string {
    return this.formatBytes(connection.bytesSent + connection.bytesReceived);
  }

  protected formatDate(value: string | null): string {
    if (!value) {
      return 'Pending';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return 'Unknown';
    }

    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(date);
  }

  protected formatRelativeTime(value: string | null): string {
    if (!value) {
      return 'Pending';
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

  private formatBytes(value: number): string {
    if (value < 1024) {
      return `${value} B`;
    }

    const units = ['KB', 'MB', 'GB', 'TB'];
    let scaledValue = value / 1024;
    let unitIndex = 0;

    while (scaledValue >= 1024 && unitIndex < units.length - 1) {
      scaledValue /= 1024;
      unitIndex++;
    }

    return `${scaledValue.toFixed(scaledValue >= 10 ? 0 : 1)} ${units[unitIndex]}`;
  }
}
