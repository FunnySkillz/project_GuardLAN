import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';

import { DashboardFacade } from './features/dashboard/data-access/dashboard.facade';
import {
  AlertDto,
  DeviceDto,
  NetworkScanDto
} from './features/dashboard/models/dashboard-overview';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App implements OnInit {
  private readonly defaultSubnet = '192.168.1.0/24';
  protected readonly dashboard = inject(DashboardFacade);
  protected readonly summary = computed(() => this.dashboard.data()?.summary ?? null);
  protected readonly devices = this.dashboard.devices;
  protected readonly alerts = this.dashboard.alerts;
  protected readonly domains = this.dashboard.domains;
  protected readonly scans = this.dashboard.scans;
  protected readonly loading = this.dashboard.loading;
  protected readonly error = this.dashboard.error;
  protected readonly queueingScan = this.dashboard.queueingScan;
  protected readonly subnet = this.defaultSubnet;

  ngOnInit(): void {
    this.dashboard.load();
  }

  protected retry(): void {
    this.dashboard.load();
  }

  protected startScan(): void {
    this.dashboard.queueScan(this.defaultSubnet);
  }

  protected deviceName(device: DeviceDto): string {
    return device.hostname?.trim() || device.ipAddress;
  }

  protected deviceVendor(device: DeviceDto): string {
    return device.vendor?.trim() || 'Unknown vendor';
  }

  protected deviceTypeLabel(device: DeviceDto): string {
    switch (device.deviceType) {
      case 'SmartTv':
        return 'Smart TV';
      case 'Iot':
        return 'IoT';
      default:
        return device.deviceType;
    }
  }

  protected deviceTraffic(deviceId: string): string {
    const activity = this.summary()?.mostActiveDevices.find((device) => device.deviceId === deviceId);

    if (!activity) {
      return '0 MB';
    }

    return this.formatBytes(activity.bytesSent + activity.bytesReceived);
  }

  protected alertDevice(alert: AlertDto): string {
    if (!alert.deviceId) {
      return 'Network';
    }

    const device = this.devices().find((candidate) => candidate.id === alert.deviceId);

    return device ? this.deviceName(device) : alert.deviceId;
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
