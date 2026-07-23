import { Component } from '@angular/core';

interface DeviceRow {
  name: string;
  ipAddress: string;
  macAddress: string;
  vendor: string;
  type: string;
  trusted: boolean;
  online: boolean;
  lastSeen: string;
  trafficMb: number;
}

interface AlertRow {
  severity: 'High' | 'Medium' | 'Low';
  type: string;
  device: string;
  created: string;
}

interface DomainRow {
  domain: string;
  requests: number;
  blocked: number;
}

interface ScanRow {
  subnet: string;
  status: string;
  discovered: number;
  requested: string;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly devices: DeviceRow[] = [
    {
      name: 'gateway',
      ipAddress: '192.168.1.1',
      macAddress: '02:00:00:00:00:01',
      vendor: 'OPNsense',
      type: 'Router',
      trusted: true,
      online: true,
      lastSeen: '1 min ago',
      trafficMb: 824
    },
    {
      name: 'desktop-pc',
      ipAddress: '192.168.1.22',
      macAddress: '02:00:00:00:00:22',
      vendor: 'Intel',
      type: 'Desktop',
      trusted: true,
      online: true,
      lastSeen: '3 min ago',
      trafficMb: 2627
    },
    {
      name: 'living-room-tv',
      ipAddress: '192.168.1.32',
      macAddress: 'AA:BB:CC:DD:EE:FF',
      vendor: 'Samsung',
      type: 'Smart TV',
      trusted: true,
      online: true,
      lastSeen: '2 min ago',
      trafficMb: 85373
    },
    {
      name: 'Unidentified',
      ipAddress: '192.168.1.71',
      macAddress: '02:00:00:00:00:71',
      vendor: 'Unknown',
      type: 'Unknown',
      trusted: false,
      online: true,
      lastSeen: '4 min ago',
      trafficMb: 197
    }
  ];

  protected readonly alerts: AlertRow[] = [
    {
      severity: 'High',
      type: 'Unknown device connected',
      device: '192.168.1.71',
      created: '2 hours ago'
    },
    {
      severity: 'Medium',
      type: 'New domain observed',
      device: 'Unidentified',
      created: '8 min ago'
    },
    {
      severity: 'Low',
      type: 'Device disappeared',
      device: 'garage-camera',
      created: '1 day ago'
    }
  ];

  protected readonly domains: DomainRow[] = [
    { domain: 'streaming.example', requests: 312, blocked: 0 },
    { domain: 'github.com', requests: 188, blocked: 0 },
    { domain: 'ads.streaming.example', requests: 73, blocked: 73 },
    { domain: 'new-device-check.example', requests: 12, blocked: 0 }
  ];

  protected readonly scans: ScanRow[] = [
    { subnet: '192.168.1.0/24', status: 'Completed', discovered: 4, requested: '5 min ago' },
    { subnet: '192.168.1.0/24', status: 'Completed', discovered: 3, requested: '65 min ago' }
  ];

  protected get onlineDevices(): number {
    return this.devices.filter((device) => device.online).length;
  }

  protected get unknownDevices(): number {
    return this.devices.filter((device) => !device.trusted || device.type === 'Unknown').length;
  }

  protected get trustedDevices(): number {
    return this.devices.filter((device) => device.trusted).length;
  }

  protected get blockedDomains(): number {
    return this.domains.reduce((total, domain) => total + domain.blocked, 0);
  }

  protected formatTraffic(megabytes: number): string {
    if (megabytes >= 1024) {
      return `${(megabytes / 1024).toFixed(1)} GB`;
    }

    return `${megabytes} MB`;
  }
}
