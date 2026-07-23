import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { DnsFilter, DnsFacade } from '../data-access/dns.facade';
import { DnsClientSummaryDto, DnsQueryDto } from '../models/dns-overview';
import { LiveUpdatesService } from '../../../shared/live-updates/live-updates.service';

interface DnsFilterOption {
  readonly value: DnsFilter;
  readonly label: string;
}

@Component({
  selector: 'app-dns-page',
  templateUrl: './dns-page.component.html',
  styleUrl: './dns-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DnsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly liveUpdates = inject(LiveUpdatesService);
  protected readonly facade = inject(DnsFacade);
  protected readonly filters: readonly DnsFilterOption[] = [
    { value: 'all', label: 'All' },
    { value: 'blocked', label: 'Blocked' },
    { value: 'allowed', label: 'Allowed' }
  ];

  ngOnInit(): void {
    this.liveUpdates
      .ofTypes('dnsIngestionCompleted')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.facade.load());

    this.facade.load();
  }

  protected retry(): void {
    this.facade.load();
  }

  protected setFilter(filter: DnsFilter): void {
    this.facade.setFilter(filter);
  }

  protected updateSearch(event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.facade.setSearch(target.value);
    }
  }

  protected isFilterActive(filter: DnsFilter): boolean {
    return this.facade.filter() === filter;
  }

  protected queryDevice(query: DnsQueryDto): string {
    return query.deviceName ?? query.clientIp;
  }

  protected clientName(client: DnsClientSummaryDto): string {
    return client.deviceName ?? client.clientIp;
  }

  protected blockRate(blockedRequests: number, requests: number): string {
    if (requests === 0) {
      return '0%';
    }

    return `${Math.round((blockedRequests / requests) * 100)}%`;
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
}
