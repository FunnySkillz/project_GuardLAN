import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { DeviceEvidenceFacade } from '../data-access/device-evidence.facade';
import { ConnectionDto } from '../../connections/models/connection-overview';
import { DnsQueryDto } from '../../dns/models/dns-overview';
import {
  DeviceDto,
  deviceDisplayName,
  deviceTypeLabel,
  riskReason
} from '../../../shared/models/network-device';
import { AlertDto, alertTypeLabel, isOpenAlert } from '../../../shared/models/security-alert';
import { LiveUpdatesService } from '../../../shared/live-updates/live-updates.service';

@Component({
  selector: 'app-device-evidence-page',
  imports: [RouterLink],
  templateUrl: './device-evidence-page.component.html',
  styleUrl: './device-evidence-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeviceEvidencePageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly liveUpdates = inject(LiveUpdatesService);
  protected readonly facade = inject(DeviceEvidenceFacade);
  private deviceId = '';

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.deviceId = params.get('id') ?? '';

      if (this.deviceId) {
        this.facade.load(this.deviceId);
      }
    });

    this.liveUpdates
      .ofTypes(
        'alertResolved',
        'deviceStatusChanged',
        'dnsIngestionCompleted',
        'newAlert',
        'newDevice',
        'scanCompleted'
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.retry());
  }

  protected retry(): void {
    if (this.deviceId) {
      this.facade.load(this.deviceId);
    }
  }

  protected pageTitle(): string {
    const evidence = this.facade.evidence();

    return evidence ? this.deviceName(evidence.device) : 'Device detail';
  }

  protected deviceName(device: DeviceDto): string {
    return deviceDisplayName(device);
  }

  protected deviceType(device: DeviceDto): string {
    return deviceTypeLabel(device.deviceType);
  }

  protected riskReason(device: DeviceDto): string {
    return riskReason(device);
  }

  protected alertType(alert: AlertDto): string {
    return alertTypeLabel(alert.type);
  }

  protected alertStatus(alert: AlertDto): string {
    return isOpenAlert(alert) ? 'Open' : 'Resolved';
  }

  protected dnsStatus(query: DnsQueryDto): string {
    return query.wasBlocked ? 'Blocked' : 'Allowed';
  }

  protected destination(connection: ConnectionDto): string {
    return connection.destinationDomain ?? connection.destinationIp;
  }

  protected endpoint(connection: ConnectionDto): string {
    return connection.destinationPort === null
      ? connection.protocol
      : `${connection.protocol}/${connection.destinationPort}`;
  }

  protected totalTraffic(bytesSent: number, bytesReceived: number): string {
    return this.formatBytes(bytesSent + bytesReceived);
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

  protected formatBytes(value: number): string {
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
