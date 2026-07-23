import { DeviceDto } from '../../../shared/models/network-device';
import { AlertDto } from '../../../shared/models/security-alert';

export type NetworkScanStatus = 'Queued' | 'Running' | 'Completed' | 'Failed';

export interface DashboardOverviewDto {
  readonly summary: DashboardSnapshotDto;
  readonly devices: readonly DeviceDto[];
  readonly recentScans: readonly NetworkScanDto[];
}

export interface DashboardSnapshotDto {
  readonly onlineDevices: number;
  readonly unknownDevices: number;
  readonly newDevicesToday: number;
  readonly trustedDevices: number;
  readonly dnsRequestsToday: number;
  readonly blockedDomainsToday: number;
  readonly openAlerts: number;
  readonly criticalAlerts: number;
  readonly connectionTraffic: TrafficSummaryDto;
  readonly mostActiveDevices: readonly DeviceActivityDto[];
  readonly topConnectionProtocols: readonly ProtocolActivityDto[];
  readonly mostContactedExternalDomains: readonly DomainActivityDto[];
  readonly recentAlerts: readonly AlertDto[];
}

export interface DeviceActivityDto {
  readonly deviceId: string;
  readonly name: string;
  readonly ipAddress: string;
  readonly bytesSent: number;
  readonly bytesReceived: number;
  readonly connectionCount: number;
}

export interface TrafficSummaryDto {
  readonly totalConnections: number;
  readonly activeDevices: number;
  readonly uniqueDestinations: number;
  readonly bytesSent: number;
  readonly bytesReceived: number;
}

export interface ProtocolActivityDto {
  readonly protocol: string;
  readonly connections: number;
  readonly bytesSent: number;
  readonly bytesReceived: number;
}

export interface DomainActivityDto {
  readonly domain: string;
  readonly requests: number;
  readonly blockedRequests: number;
}

export interface NetworkScanDto {
  readonly id: string;
  readonly subnet: string;
  readonly status: NetworkScanStatus;
  readonly requestedUtc: string;
  readonly startedUtc: string | null;
  readonly finishedUtc: string | null;
  readonly devicesDiscovered: number;
  readonly notes: string | null;
}

export interface QueueNetworkScanRequest {
  readonly subnet: string | null;
}
