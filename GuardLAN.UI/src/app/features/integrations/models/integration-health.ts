export type IntegrationKind = 'Dns' | 'Zeek' | 'Suricata';

export type IntegrationHealthStatus = 'Healthy' | 'Warning' | 'Unavailable' | 'Disabled' | 'Stale';

export interface IntegrationHealthOverviewDto {
  readonly summary: IntegrationHealthSummaryDto;
  readonly sources: readonly IntegrationHealthDto[];
  readonly recentRuns: readonly IntegrationImportRunDto[];
}

export interface IntegrationHealthSummaryDto {
  readonly totalSources: number;
  readonly healthySources: number;
  readonly warningSources: number;
  readonly unavailableSources: number;
  readonly disabledSources: number;
  readonly staleSources: number;
  readonly lastCheckedUtc: string | null;
}

export interface IntegrationHealthDto {
  readonly id: string;
  readonly source: string;
  readonly kind: IntegrationKind;
  readonly status: IntegrationHealthStatus;
  readonly sourceEnabled: boolean;
  readonly sourceAvailable: boolean;
  readonly lastCheckedUtc: string;
  readonly staleAfterUtc: string | null;
  readonly lastSuccessUtc: string | null;
  readonly lastFailureUtc: string | null;
  readonly recordsRead: number;
  readonly recordsImported: number;
  readonly recordsRejected: number;
  readonly message: string;
}

export interface IntegrationImportRunDto {
  readonly id: string;
  readonly source: string;
  readonly kind: IntegrationKind;
  readonly status: IntegrationHealthStatus;
  readonly sourceEnabled: boolean;
  readonly sourceAvailable: boolean;
  readonly completedUtc: string;
  readonly recordsRead: number;
  readonly recordsImported: number;
  readonly recordsRejected: number;
  readonly message: string;
}

export type IntegrationImportTarget = 'pihole' | 'zeek' | 'suricata';
