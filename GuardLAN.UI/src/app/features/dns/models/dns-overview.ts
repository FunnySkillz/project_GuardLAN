export interface DnsOverviewDto {
  readonly summary: DnsOverviewSummaryDto;
  readonly topDomains: readonly DnsDomainSummaryDto[];
  readonly topClients: readonly DnsClientSummaryDto[];
  readonly recentQueries: readonly DnsQueryDto[];
}

export interface DnsOverviewSummaryDto {
  readonly totalQueries: number;
  readonly allowedQueries: number;
  readonly blockedQueries: number;
  readonly uniqueDomains: number;
  readonly activeClients: number;
}

export interface DnsDomainSummaryDto {
  readonly domain: string;
  readonly requests: number;
  readonly blockedRequests: number;
}

export interface DnsClientSummaryDto {
  readonly deviceId: string | null;
  readonly deviceName: string | null;
  readonly clientIp: string;
  readonly requests: number;
  readonly blockedRequests: number;
}

export interface DnsQueryDto {
  readonly id: string;
  readonly deviceId: string | null;
  readonly deviceName: string | null;
  readonly clientIp: string;
  readonly domain: string;
  readonly wasBlocked: boolean;
  readonly timestampUtc: string;
}
