export type AlertSeverity = 'Low' | 'Medium' | 'High' | 'Critical';

export interface AlertDto {
  readonly id: string;
  readonly deviceId: string | null;
  readonly deviceName: string | null;
  readonly deviceIpAddress: string | null;
  readonly deviceMacAddress: string | null;
  readonly severity: AlertSeverity;
  readonly type: string;
  readonly message: string;
  readonly createdUtc: string;
  readonly resolvedUtc: string | null;
}

export function alertTypeLabel(type: string): string {
  switch (type) {
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
