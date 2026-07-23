import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { IntegrationsFacade } from '../data-access/integrations.facade';
import {
  IntegrationHealthDto,
  IntegrationHealthStatus,
  IntegrationKind
} from '../models/integration-health';
import { LiveUpdatesService } from '../../../shared/live-updates/live-updates.service';

@Component({
  selector: 'app-integrations-page',
  templateUrl: './integrations-page.component.html',
  styleUrl: './integrations-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IntegrationsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly liveUpdates = inject(LiveUpdatesService);
  protected readonly facade = inject(IntegrationsFacade);

  ngOnInit(): void {
    this.liveUpdates
      .ofTypes('dnsIngestionCompleted', 'newAlert')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.facade.load());

    this.facade.load();
  }

  protected retry(): void {
    this.facade.load();
  }

  protected kindLabel(kind: IntegrationKind): string {
    switch (kind) {
      case 'Dns':
        return 'DNS';
      case 'Zeek':
        return 'Zeek';
      case 'Suricata':
        return 'Suricata';
    }
  }

  protected statusClass(status: IntegrationHealthStatus): string {
    return status.toLowerCase();
  }

  protected statusLabel(source: IntegrationHealthDto): string {
    if (source.status === 'Disabled') {
      return 'Disabled';
    }

    if (source.status === 'Unavailable') {
      return 'Unavailable';
    }

    if (source.status === 'Warning') {
      return 'Warning';
    }

    return 'Healthy';
  }

  protected sourceState(source: IntegrationHealthDto): string {
    if (!source.sourceEnabled) {
      return 'Off';
    }

    return source.sourceAvailable ? 'Available' : 'Missing';
  }

  protected recordFlow(source: IntegrationHealthDto): string {
    return `${source.recordsImported} imported / ${source.recordsRead} read`;
  }

  protected formatRelativeTime(value: string | null): string {
    if (!value) {
      return 'Never';
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
