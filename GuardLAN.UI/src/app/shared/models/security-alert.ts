export type AlertSeverity = 'Low' | 'Medium' | 'High' | 'Critical';

export interface AlertDto {
  readonly id: string;
  readonly deviceId: string | null;
  readonly deviceName: string | null;
  readonly deviceIpAddress: string | null;
  readonly deviceMacAddress: string | null;
  readonly connectionId: string | null;
  readonly source: string | null;
  readonly sourceRecordId: string | null;
  readonly sourceIp: string | null;
  readonly destinationIp: string | null;
  readonly destinationPort: number | null;
  readonly protocol: string | null;
  readonly severity: AlertSeverity;
  readonly type: string;
  readonly message: string;
  readonly createdUtc: string;
  readonly resolvedUtc: string | null;
  readonly evidenceSummary: string | null;
  readonly history: readonly AlertHistoryDto[];
}

export interface AlertHistoryDto {
  readonly id: string;
  readonly eventType: string;
  readonly description: string;
  readonly createdUtc: string;
}

export function alertTypeLabel(type: string): string {
  switch (type) {
    case 'IdsAlert':
      return 'IDS alert';
    case 'UnknownDeviceConnected':
      return 'Unknown device connected';
    case 'DeviceDisappeared':
      return 'Device disappeared';
    case 'NewDomainObserved':
      return 'New domain observed';
    default:
      return type.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}

export function severityRank(severity: AlertSeverity): number {
  switch (severity) {
    case 'Critical':
      return 4;
    case 'High':
      return 3;
    case 'Medium':
      return 2;
    case 'Low':
      return 1;
  }
}

export function isOpenAlert(alert: AlertDto): boolean {
  return alert.resolvedUtc === null;
}
