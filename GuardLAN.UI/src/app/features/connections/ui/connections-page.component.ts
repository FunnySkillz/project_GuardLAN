import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';

import {
  ConnectionProtocolFilter,
  ConnectionsFacade
} from '../data-access/connections.facade';
import {
  ConnectionDestinationSummaryDto,
  ConnectionDto,
  ConnectionProtocolSummaryDto
} from '../models/connection-overview';

interface ConnectionFilterOption {
  readonly value: ConnectionProtocolFilter;
  readonly label: string;
}

@Component({
  selector: 'app-connections-page',
  templateUrl: './connections-page.component.html',
  styleUrl: './connections-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConnectionsPageComponent implements OnInit {
  protected readonly facade = inject(ConnectionsFacade);
  protected readonly filters: readonly ConnectionFilterOption[] = [
    { value: 'all', label: 'All' },
    { value: 'tcp', label: 'TCP' },
    { value: 'udp', label: 'UDP' },
    { value: 'other', label: 'Other' }
  ];

  ngOnInit(): void {
    this.facade.load();
  }

  protected retry(): void {
    this.facade.load();
  }

  protected setFilter(filter: ConnectionProtocolFilter): void {
    this.facade.setFilter(filter);
  }

  protected updateSearch(event: Event): void {
    const target = event.target;

    if (target instanceof HTMLInputElement) {
      this.facade.setSearch(target.value);
    }
  }

  protected isFilterActive(filter: ConnectionProtocolFilter): boolean {
    return this.facade.filter() === filter;
  }

  protected destination(connection: ConnectionDto): string {
    return connection.destinationDomain ?? connection.destinationIp;
  }

  protected endpoint(connection: ConnectionDto): string {
    return connection.destinationPort === null
      ? connection.protocol
      : `${connection.protocol}/${connection.destinationPort}`;
  }

  protected totalBytesSent(): number {
    return this.facade.summary().totalBytesSent;
  }

  protected totalBytesReceived(): number {
    return this.facade.summary().totalBytesReceived;
  }

  protected protocolTraffic(protocol: ConnectionProtocolSummaryDto): string {
    return this.formatBytes(protocol.bytesSent + protocol.bytesReceived);
  }

  protected destinationTraffic(destination: ConnectionDestinationSummaryDto): string {
    return this.formatBytes(destination.bytesSent + destination.bytesReceived);
  }

  protected connectionTraffic(connection: ConnectionDto): string {
    return this.formatBytes(connection.bytesSent + connection.bytesReceived);
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
