export type DeviceType =
  | 'Unknown'
  | 'Desktop'
  | 'Laptop'
  | 'Phone'
  | 'Tablet'
  | 'SmartTv'
  | 'Iot'
  | 'Router'
  | 'Printer'
  | 'Server';

export type AlertSeverity = 'Low' | 'Medium' | 'High' | 'Critical';

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
  readonly mostActiveDevices: readonly DeviceActivityDto[];
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

export interface DomainActivityDto {
  readonly domain: string;
  readonly requests: number;
  readonly blockedRequests: number;
}

export interface AlertDto {
  readonly id: string;
  readonly deviceId: string | null;
  readonly severity: AlertSeverity;
  readonly type: string;
  readonly message: string;
  readonly createdUtc: string;
  readonly resolvedUtc: string | null;
}

export interface DeviceDto {
  readonly id: string;
  readonly ipAddress: string;
  readonly macAddress: string;
  readonly hostname: string | null;
  readonly vendor: string | null;
  readonly deviceType: DeviceType;
  readonly isTrusted: boolean;
  readonly firstSeenUtc: string;
  readonly lastSeenUtc: string;
  readonly isOnline: boolean;
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
