import { ConnectionDto } from '../../connections/models/connection-overview';
import { DnsQueryDto } from '../../dns/models/dns-overview';
import { AlertDto } from '../../../shared/models/security-alert';
import { DeviceDto } from '../../../shared/models/network-device';

export interface DeviceEvidenceDto {
  readonly device: DeviceDto;
  readonly summary: DeviceEvidenceSummaryDto;
  readonly recentAlerts: readonly AlertDto[];
  readonly recentDnsQueries: readonly DnsQueryDto[];
  readonly recentConnections: readonly ConnectionDto[];
}

export interface DeviceEvidenceSummaryDto {
  readonly sinceUtc: string;
  readonly alerts: number;
  readonly openAlerts: number;
  readonly dnsQueries: number;
  readonly blockedDnsQueries: number;
  readonly uniqueDomains: number;
  readonly connections: number;
  readonly uniqueDestinations: number;
  readonly bytesSent: number;
  readonly bytesReceived: number;
}
