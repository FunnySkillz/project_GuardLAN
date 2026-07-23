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

export interface UpdateDeviceRequest {
  readonly hostname?: string | null;
  readonly deviceType?: DeviceType | null;
  readonly isTrusted?: boolean | null;
}

export interface DeviceTypeOption {
  readonly value: DeviceType;
  readonly label: string;
}

export const DEVICE_TYPE_OPTIONS: readonly DeviceTypeOption[] = [
  { value: 'Unknown', label: 'Unknown' },
  { value: 'Desktop', label: 'Desktop' },
  { value: 'Laptop', label: 'Laptop' },
  { value: 'Phone', label: 'Phone' },
  { value: 'Tablet', label: 'Tablet' },
  { value: 'SmartTv', label: 'Smart TV' },
  { value: 'Iot', label: 'IoT' },
  { value: 'Router', label: 'Router' },
  { value: 'Printer', label: 'Printer' },
  { value: 'Server', label: 'Server' }
];

export function deviceDisplayName(device: DeviceDto): string {
  return device.hostname?.trim() || device.ipAddress;
}

export function deviceTypeLabel(deviceType: DeviceType): string {
  return DEVICE_TYPE_OPTIONS.find((option) => option.value === deviceType)?.label ?? deviceType;
}

export function needsDeviceReview(device: DeviceDto): boolean {
  return !device.isTrusted || device.deviceType === 'Unknown';
}

export function isDeviceType(value: string): value is DeviceType {
  return DEVICE_TYPE_OPTIONS.some((option) => option.value === value);
}
