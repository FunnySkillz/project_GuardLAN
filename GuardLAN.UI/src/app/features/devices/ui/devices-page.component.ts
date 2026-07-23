import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { DeviceFilter, DevicesFacade } from '../data-access/devices.facade';
import {
  DEVICE_TYPE_OPTIONS,
  DeviceDto,
  DeviceType,
  deviceDisplayName,
  deviceTypeLabel,
  riskReason,
  isDeviceType,
  needsDeviceReview
} from '../../../shared/models/network-device';
import { LiveUpdatesService } from '../../../shared/live-updates/live-updates.service';

interface DeviceDraft {
  readonly hostname: string;
  readonly deviceType: DeviceType;
  readonly isTrusted: boolean;
}

interface DeviceFilterOption {
  readonly value: DeviceFilter;
  readonly label: string;
}

@Component({
  selector: 'app-devices-page',
  templateUrl: './devices-page.component.html',
  styleUrl: './devices-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DevicesPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly liveUpdates = inject(LiveUpdatesService);
  protected readonly facade = inject(DevicesFacade);
  protected readonly deviceTypes = DEVICE_TYPE_OPTIONS;
  protected readonly filters: readonly DeviceFilterOption[] = [
    { value: 'all', label: 'All' },
    { value: 'review', label: 'Review' },
    { value: 'risk', label: 'Risk' },
    { value: 'offline', label: 'Offline' },
    { value: 'trusted', label: 'Trusted' }
  ];
  private readonly drafts = signal<Record<string, DeviceDraft>>({});

  ngOnInit(): void {
    this.liveUpdates
      .ofTypes('deviceStatusChanged', 'newDevice', 'scanCompleted')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.facade.load());

    this.facade.load();
  }

  protected retry(): void {
    this.facade.load();
  }

  protected setFilter(filter: DeviceFilter): void {
    this.facade.setFilter(filter);
  }

  protected updateSearch(event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.facade.setSearch(target.value);
    }
  }

  protected updateHostname(device: DeviceDto, event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.updateDraft(device, { hostname: target.value });
    }
  }

  protected updateDeviceType(device: DeviceDto, event: Event): void {
    const target = event.target;

    if (target instanceof HTMLSelectElement && isDeviceType(target.value)) {
      this.updateDraft(device, { deviceType: target.value });
    }
  }

  protected updateTrust(device: DeviceDto, event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.updateDraft(device, { isTrusted: target.checked });
    }
  }

  protected save(device: DeviceDto): void {
    const draft = this.getDraft(device);
    const hostname = draft.hostname.trim();

    this.facade.saveDevice(device.id, {
      hostname: hostname.length > 0 ? hostname : null,
      deviceType: draft.deviceType,
      isTrusted: draft.isTrusted
    });
  }

  protected reset(device: DeviceDto): void {
    const nextDrafts = { ...this.drafts() };
    delete nextDrafts[device.id];
    this.drafts.set(nextDrafts);
  }

  protected deviceName(device: DeviceDto): string {
    return deviceDisplayName(device);
  }

  protected deviceVendor(device: DeviceDto): string {
    return device.vendor?.trim() || 'Unknown vendor';
  }

  protected typeLabel(deviceType: DeviceType): string {
    return deviceTypeLabel(deviceType);
  }

  protected isFilterActive(filter: DeviceFilter): boolean {
    return this.facade.filter() === filter;
  }

  protected draftHostname(device: DeviceDto): string {
    return this.getDraft(device).hostname;
  }

  protected draftDeviceType(device: DeviceDto): DeviceType {
    return this.getDraft(device).deviceType;
  }

  protected draftTrusted(device: DeviceDto): boolean {
    return this.getDraft(device).isTrusted;
  }

  protected hasDraftChanges(device: DeviceDto): boolean {
    const draft = this.getDraft(device);

    return (
      draft.hostname.trim() !== (device.hostname ?? '') ||
      draft.deviceType !== device.deviceType ||
      draft.isTrusted !== device.isTrusted
    );
  }

  protected saving(device: DeviceDto): boolean {
    return this.facade.savingDeviceId() === device.id;
  }

  protected needsReview(device: DeviceDto): boolean {
    return needsDeviceReview(device);
  }

  protected riskReason(device: DeviceDto): string {
    return riskReason(device);
  }

  protected formatRelativeTime(value: string): string {
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

  private updateDraft(device: DeviceDto, changes: Partial<DeviceDraft>): void {
    this.drafts.update((drafts) => ({
      ...drafts,
      [device.id]: {
        ...this.createDraft(device),
        ...drafts[device.id],
        ...changes
      }
    }));
  }

  private getDraft(device: DeviceDto): DeviceDraft {
    return this.drafts()[device.id] ?? this.createDraft(device);
  }

  private createDraft(device: DeviceDto): DeviceDraft {
    return {
      hostname: device.hostname ?? '',
      deviceType: device.deviceType,
      isTrusted: device.isTrusted
    };
  }
}
