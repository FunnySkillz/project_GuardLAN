import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { DevicesApi } from './devices.api';
import { DeviceDto, needsDeviceReview, UpdateDeviceRequest } from '../../../shared/models/network-device';

export type DeviceFilter = 'all' | 'review' | 'offline' | 'trusted';

interface DevicesState {
  readonly devices: readonly DeviceDto[];
  readonly error: string | null;
  readonly filter: DeviceFilter;
  readonly loading: boolean;
  readonly savingDeviceId: string | null;
  readonly search: string;
}

@Injectable({ providedIn: 'root' })
export class DevicesFacade {
  private readonly api = inject(DevicesApi);
  private readonly state = signal<DevicesState>({
    devices: [],
    error: null,
    filter: 'all',
    loading: false,
    savingDeviceId: null,
    search: ''
  });

  readonly devices = computed(() => this.state().devices);
  readonly error = computed(() => this.state().error);
  readonly filter = computed(() => this.state().filter);
  readonly loading = computed(() => this.state().loading);
  readonly savingDeviceId = computed(() => this.state().savingDeviceId);
  readonly search = computed(() => this.state().search);
  readonly filteredDevices = computed(() => {
    const state = this.state();
    const query = state.search.trim().toLowerCase();

    return state.devices.filter((device) => {
      if (!matchesFilter(device, state.filter)) {
        return false;
      }

      if (!query) {
        return true;
      }

      return [
        device.hostname,
        device.ipAddress,
        device.macAddress,
        device.vendor,
        device.deviceType
      ].some((value) => value?.toLowerCase().includes(query));
    });
  });
  readonly summary = computed(() => {
    const devices = this.state().devices;

    return {
      total: devices.length,
      online: devices.filter((device) => device.isOnline).length,
      review: devices.filter(needsDeviceReview).length,
      offline: devices.filter((device) => !device.isOnline).length,
      trusted: devices.filter((device) => device.isTrusted).length
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
        next: (devices) => this.state.update((state) => ({ ...state, devices })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Device inventory could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }

  setFilter(filter: DeviceFilter): void {
    this.state.update((state) => ({ ...state, filter }));
  }

  setSearch(search: string): void {
    this.state.update((state) => ({ ...state, search }));
  }

  saveDevice(deviceId: string, request: UpdateDeviceRequest): void {
    this.state.update((state) => ({ ...state, error: null, savingDeviceId: deviceId }));

    this.api
      .update(deviceId, request)
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, savingDeviceId: null })))
      )
      .subscribe({
        next: (updatedDevice) =>
          this.state.update((state) => ({
            ...state,
            devices: state.devices.map((device) =>
              device.id === updatedDevice.id ? updatedDevice : device
            )
          })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Device changes could not be saved. Check the API and try again.'
          }))
      });
  }
}

function matchesFilter(device: DeviceDto, filter: DeviceFilter): boolean {
  switch (filter) {
    case 'review':
      return needsDeviceReview(device);
    case 'offline':
      return !device.isOnline;
    case 'trusted':
      return device.isTrusted;
    case 'all':
      return true;
  }
}
