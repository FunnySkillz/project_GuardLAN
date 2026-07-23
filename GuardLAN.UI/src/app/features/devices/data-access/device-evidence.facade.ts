import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, take } from 'rxjs';

import { DevicesApi } from './devices.api';
import { DeviceEvidenceDto } from '../models/device-evidence';

interface DeviceEvidenceState {
  readonly evidence: DeviceEvidenceDto | null;
  readonly error: string | null;
  readonly loading: boolean;
}

@Injectable({ providedIn: 'root' })
export class DeviceEvidenceFacade {
  private readonly api = inject(DevicesApi);
  private readonly state = signal<DeviceEvidenceState>({
    evidence: null,
    error: null,
    loading: false
  });

  readonly evidence = computed(() => this.state().evidence);
  readonly error = computed(() => this.state().error);
  readonly loading = computed(() => this.state().loading);

  load(deviceId: string): void {
    this.state.update((state) => ({ ...state, error: null, loading: true }));

    this.api
      .evidence(deviceId)
      .pipe(
        take(1),
        finalize(() => this.state.update((state) => ({ ...state, loading: false })))
      )
      .subscribe({
        next: (evidence) => this.state.update((state) => ({ ...state, evidence })),
        error: () =>
          this.state.update((state) => ({
            ...state,
            error: 'Device evidence could not be loaded. Check that the GuardLAN API is running.'
          }))
      });
  }
}
