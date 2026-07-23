import { computed, inject, Injectable, NgZone, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { filter, Observable, Subject } from 'rxjs';

export type LiveUpdateType =
  | 'alertResolved'
  | 'alertUpdated'
  | 'deviceStatusChanged'
  | 'dnsIngestionCompleted'
  | 'newAlert'
  | 'newDevice'
  | 'scanCompleted'
  | 'scanFailed'
  | 'scanQueued';

export interface LiveUpdateDto {
  readonly type: LiveUpdateType;
  readonly message: string;
  readonly createdUtc: string;
  readonly deviceId: string | null;
  readonly alertId: string | null;
  readonly scanRunId: string | null;
  readonly status: string | null;
  readonly source: string | null;
  readonly count: number | null;
}

interface LiveUpdateState {
  readonly connected: boolean;
  readonly connecting: boolean;
  readonly error: string | null;
  readonly lastUpdate: LiveUpdateDto | null;
}

@Injectable({ providedIn: 'root' })
export class LiveUpdatesService {
  private readonly zone = inject(NgZone);
  private readonly updatesSubject = new Subject<LiveUpdateDto>();
  private readonly state = signal<LiveUpdateState>({
    connected: false,
    connecting: false,
    error: null,
    lastUpdate: null
  });
  private connection: signalR.HubConnection | null = null;
  private retryHandle: ReturnType<typeof setTimeout> | null = null;

  readonly connected = computed(() => this.state().connected);
  readonly connecting = computed(() => this.state().connecting);
  readonly error = computed(() => this.state().error);
  readonly lastUpdate = computed(() => this.state().lastUpdate);
  readonly updates$ = this.updatesSubject.asObservable();

  connect(): void {
    if (this.connection) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/guardlan', { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    this.connection.on('liveUpdate', (update: LiveUpdateDto) => {
      this.zone.run(() => {
        this.state.update((state) => ({
          ...state,
          lastUpdate: update
        }));
        this.updatesSubject.next(update);
      });
    });

    this.connection.onreconnecting(() => {
      this.zone.run(() =>
        this.state.update((state) => ({
          ...state,
          connected: false,
          connecting: true,
          error: null
        }))
      );
    });

    this.connection.onreconnected(() => {
      this.zone.run(() =>
        this.state.update((state) => ({
          ...state,
          connected: true,
          connecting: false,
          error: null
        }))
      );
    });

    this.connection.onclose(() => {
      if (!this.connection) {
        return;
      }

      this.zone.run(() =>
        this.state.update((state) => ({
          ...state,
          connected: false,
          connecting: false
        }))
      );
      this.scheduleReconnect();
    });

    void this.startConnection();
  }

  disconnect(): void {
    if (this.retryHandle) {
      clearTimeout(this.retryHandle);
      this.retryHandle = null;
    }

    const connection = this.connection;
    this.connection = null;

    if (connection) {
      void connection.stop();
    }

    this.state.update((state) => ({
      ...state,
      connected: false,
      connecting: false,
      error: null
    }));
  }

  ofTypes(...types: readonly LiveUpdateType[]): Observable<LiveUpdateDto> {
    return this.updates$.pipe(filter((update) => types.includes(update.type)));
  }

  private async startConnection(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    this.state.update((state) => ({
      ...state,
      connected: false,
      connecting: true,
      error: null
    }));

    try {
      await this.connection.start();

      this.zone.run(() =>
        this.state.update((state) => ({
          ...state,
          connected: true,
          connecting: false,
          error: null
        }))
      );
    } catch {
      this.zone.run(() =>
        this.state.update((state) => ({
          ...state,
          connected: false,
          connecting: false,
          error: 'Live updates are unavailable.'
        }))
      );
      this.scheduleReconnect();
    }
  }

  private scheduleReconnect(): void {
    if (!this.connection || this.retryHandle) {
      return;
    }

    this.retryHandle = setTimeout(() => {
      this.retryHandle = null;
      void this.startConnection();
    }, 5000);
  }
}
