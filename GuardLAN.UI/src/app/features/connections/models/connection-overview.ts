export interface ConnectionOverviewDto {
  readonly summary: ConnectionOverviewSummaryDto;
  readonly topProtocols: readonly ConnectionProtocolSummaryDto[];
  readonly topDestinations: readonly ConnectionDestinationSummaryDto[];
  readonly topDevices: readonly ConnectionDeviceSummaryDto[];
  readonly recentConnections: readonly ConnectionDto[];
}

export interface ConnectionOverviewSummaryDto {
  readonly totalConnections: number;
  readonly activeDevices: number;
  readonly uniqueDestinations: number;
  readonly totalBytesSent: number;
  readonly totalBytesReceived: number;
}

export interface ConnectionProtocolSummaryDto {
  readonly protocol: string;
  readonly connections: number;
  readonly bytesSent: number;
  readonly bytesReceived: number;
}

export interface ConnectionDestinationSummaryDto {
  readonly destination: string;
  readonly destinationIp: string;
  readonly connections: number;
  readonly bytesSent: number;
  readonly bytesReceived: number;
}

export interface ConnectionDeviceSummaryDto {
  readonly deviceId: string;
  readonly deviceName: string;
  readonly deviceIp: string;
  readonly connections: number;
  readonly bytesSent: number;
  readonly bytesReceived: number;
}

export interface ConnectionDto {
  readonly id: string;
  readonly deviceId: string;
  readonly deviceName: string;
  readonly deviceIp: string;
  readonly destinationIp: string;
  readonly destinationDomain: string | null;
  readonly protocol: string;
  readonly destinationPort: number | null;
  readonly bytesSent: number;
  readonly bytesReceived: number;
  readonly firstSeenUtc: string;
  readonly lastSeenUtc: string;
}
