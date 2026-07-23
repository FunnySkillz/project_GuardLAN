export type IntegrationKind = 'Dns' | 'Zeek' | 'Suricata';

export type IntegrationHealthStatus = 'Healthy' | 'Warning' | 'Unavailable' | 'Disabled';

export interface IntegrationHealthOverviewDto {
  readonly summary: IntegrationHealthSummaryDto;
  readonly sources: readonly IntegrationHealthDto[];
}

export interface IntegrationHealthSummaryDto {
  readonly totalSources: number;
  readonly healthySources: number;
  readonly warningSources: number;
  readonly unavailableSources: number;
  readonly disabledSources: number;
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
  readonly lastSuccessUtc: string | null;
  readonly lastFailureUtc: string | null;
  readonly recordsRead: number;
  readonly recordsImported: number;
  readonly recordsRejected: number;
  readonly message: string;
}
