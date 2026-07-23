import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { DashboardFacade } from '../data-access/dashboard.facade';
import { NetworkScanDto, ProtocolActivityDto } from '../models/dashboard-overview';
import {
  DeviceDto,
  deviceDisplayName,
  deviceTypeLabel,
  riskReason
} from '../../../shared/models/network-device';
import { AlertDto, alertTypeLabel } from '../../../shared/models/security-alert';
import { LiveUpdatesService } from '../../../shared/live-updates/live-updates.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [RouterLink],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent implements OnInit {
  private readonly defaultSubnet = '192.168.1.0/24';
  private readonly destroyRef = inject(DestroyRef);
  private readonly liveUpdates = inject(LiveUpdatesService);
  protected readonly dashboard = inject(DashboardFacade);
  protected readonly summary = computed(() => this.dashboard.data()?.summary ?? null);
  protected readonly devices = this.dashboard.devices;
  protected readonly alerts = this.dashboard.alerts;
  protected readonly domains = this.dashboard.domains;
  protected readonly traffic = this.dashboard.traffic;
  protected readonly protocols = this.dashboard.protocols;
  protected readonly scans = this.dashboard.scans;
  protected readonly loading = this.dashboard.loading;
  protected readonly error = this.dashboard.error;
  protected readonly queueingScan = this.dashboard.queueingScan;
  protected readonly subnet = this.defaultSubnet;

  ngOnInit(): void {
    this.liveUpdates
      .ofTypes(
        'alertResolved',
        'alertUpdated',
        'deviceStatusChanged',
        'dnsIngestionCompleted',
        'newAlert',
        'newDevice',
        'scanCompleted',
        'scanFailed',
        'scanQueued'
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.dashboard.load());

    this.dashboard.load();
  }

  protected retry(): void {
    this.dashboard.load();
  }

  protected startScan(): void {
    this.dashboard.queueScan(this.defaultSubnet);
  }

  protected deviceName(device: DeviceDto): string {
    return deviceDisplayName(device);
  }

  protected deviceVendor(device: DeviceDto): string {
    return device.vendor?.trim() || 'Unknown vendor';
  }

  protected deviceTypeLabel(device: DeviceDto): string {
    return deviceTypeLabel(device.deviceType);
  }

  protected riskReason(device: DeviceDto): string {
    return riskReason(device);
  }

  protected deviceTraffic(deviceId: string): string {
    const activity = this.summary()?.mostActiveDevices.find((device) => device.deviceId === deviceId);

    if (!activity) {
      return '0 MB';
    }

    return this.formatBytes(activity.bytesSent + activity.bytesReceived);
  }

  protected totalTraffic(): string {
    const traffic = this.traffic();

    return this.formatBytes(traffic.bytesSent + traffic.bytesReceived);
  }

  protected trafficDirection(): string {
    const traffic = this.traffic();

    return `${this.formatBytes(traffic.bytesSent)} sent | ${this.formatBytes(traffic.bytesReceived)} received`;
  }

  protected protocolTraffic(protocol: ProtocolActivityDto): string {
    return this.formatBytes(protocol.bytesSent + protocol.bytesReceived);
  }

  protected protocolShare(protocol: ProtocolActivityDto): string {
    const totalConnections = this.traffic().totalConnections;

    if (totalConnections === 0) {
      return '0%';
    }

    return `${Math.round((protocol.connections / totalConnections) * 100)}%`;
  }

  protected alertDevice(alert: AlertDto): string {
    if (alert.deviceName) {
      return alert.deviceName;
    }

    if (alert.deviceIpAddress) {
      return alert.deviceIpAddress;
    }

    if (!alert.deviceId) {
      return 'Network';
    }

    const device = this.devices().find((candidate) => candidate.id === alert.deviceId);

    return device ? this.deviceName(device) : alert.deviceId;
  }

  protected alertType(alert: AlertDto): string {
    return alertTypeLabel(alert.type);
  }

  protected scanRequested(scan: NetworkScanDto): string {
    return this.formatRelativeTime(scan.requestedUtc);
  }

  protected scanResult(scan: NetworkScanDto): string {
    if (scan.status === 'Completed') {
      return `${scan.devicesDiscovered} devices`;
    }

    return scan.status;
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

  private formatBytes(bytes: number): string {
    if (bytes >= 1024 * 1024 * 1024) {
      return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`;
    }

    if (bytes >= 1024 * 1024) {
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }

    if (bytes >= 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${bytes} B`;
  }
}
