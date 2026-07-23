import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ConnectionsApi } from './connections.api';
import { ConnectionsFacade } from './connections.facade';
import { ConnectionOverviewDto } from '../models/connection-overview';

describe('ConnectionsFacade', () => {
  const overview: ConnectionOverviewDto = {
    summary: {
      totalConnections: 2,
      activeDevices: 1,
      uniqueDestinations: 2,
      totalBytesSent: 3000,
      totalBytesReceived: 7000
    },
    topProtocols: [
      { protocol: 'TCP', connections: 1, bytesSent: 1000, bytesReceived: 5000 },
      { protocol: 'UDP', connections: 1, bytesSent: 2000, bytesReceived: 2000 }
    ],
    topDestinations: [
      {
        destination: 'github.com',
        destinationIp: '140.82.112.4',
        connections: 1,
        bytesSent: 1000,
        bytesReceived: 5000
      },
      {
        destination: 'dns.example',
        destinationIp: '203.0.113.53',
        connections: 1,
        bytesSent: 2000,
        bytesReceived: 2000
      }
    ],
    topDevices: [
      {
        deviceId: 'device-1',
        deviceName: 'desktop',
        deviceIp: '192.168.1.22',
        connections: 2,
        bytesSent: 3000,
        bytesReceived: 7000
      }
    ],
    recentConnections: [
      {
        id: 'connection-1',
        deviceId: 'device-1',
        deviceName: 'desktop',
        deviceIp: '192.168.1.22',
        destinationIp: '140.82.112.4',
        destinationDomain: 'github.com',
        protocol: 'TCP',
        destinationPort: 443,
        bytesSent: 1000,
        bytesReceived: 5000,
        firstSeenUtc: '2026-07-23T10:00:00Z',
        lastSeenUtc: '2026-07-23T10:05:00Z'
      },
      {
        id: 'connection-2',
        deviceId: 'device-1',
        deviceName: 'desktop',
        deviceIp: '192.168.1.22',
        destinationIp: '203.0.113.53',
        destinationDomain: 'dns.example',
        protocol: 'UDP',
        destinationPort: 53,
        bytesSent: 2000,
        bytesReceived: 2000,
        firstSeenUtc: '2026-07-23T10:10:00Z',
        lastSeenUtc: '2026-07-23T10:12:00Z'
      }
    ]
  };

  let api: jasmine.SpyObj<Pick<ConnectionsApi, 'getOverview'>>;
  let facade: ConnectionsFacade;

  beforeEach(() => {
    api = jasmine.createSpyObj<Pick<ConnectionsApi, 'getOverview'>>('ConnectionsApi', [
      'getOverview'
    ]);

    TestBed.configureTestingModule({
      providers: [ConnectionsFacade, { provide: ConnectionsApi, useValue: api }]
    });

    facade = TestBed.inject(ConnectionsFacade);
  });

  it('should load connection overview data', () => {
    api.getOverview.and.returnValue(of(overview));

    facade.load();

    expect(facade.summary()).toEqual(overview.summary);
    expect(facade.topProtocols()).toEqual(overview.topProtocols);
    expect(facade.filteredConnections()).toEqual(overview.recentConnections);
  });

  it('should filter UDP connections', () => {
    api.getOverview.and.returnValue(of(overview));

    facade.load();
    facade.setFilter('udp');

    expect(facade.filteredConnections()).toEqual([overview.recentConnections[1]]);
  });

  it('should search connections by destination or device', () => {
    api.getOverview.and.returnValue(of(overview));

    facade.load();
    facade.setSearch('git');

    expect(facade.filteredConnections()).toEqual([overview.recentConnections[0]]);
  });
});
