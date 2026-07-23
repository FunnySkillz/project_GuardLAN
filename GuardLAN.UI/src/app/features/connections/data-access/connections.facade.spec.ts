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
    recentConnections: {
      items: [
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
      ],
      page: 1,
      pageSize: 25,
      totalCount: 2,
      totalPages: 1
    }
  };
  const udpOverview: ConnectionOverviewDto = {
    ...overview,
    recentConnections: {
      items: [overview.recentConnections.items[1]],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1
    }
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
    expect(facade.connections()).toEqual(overview.recentConnections.items);
    expect(api.getOverview).toHaveBeenCalledWith({
      hours: 24,
      page: 1,
      pageSize: 25,
      protocol: 'all',
      search: ''
    });
  });

  it('should request UDP connections from the API', () => {
    api.getOverview.and.returnValues(of(overview), of(udpOverview));

    facade.load();
    facade.setFilter('udp');

    expect(api.getOverview.calls.mostRecent().args[0]).toEqual({
      hours: 24,
      page: 1,
      pageSize: 25,
      protocol: 'udp',
      search: ''
    });
    expect(facade.connections()).toEqual([overview.recentConnections.items[1]]);
  });

  it('should apply search through the API', () => {
    api.getOverview.and.returnValues(of(overview), of(udpOverview));

    facade.load();
    facade.setSearch('git');
    facade.applySearch();

    expect(api.getOverview.calls.mostRecent().args[0]).toEqual({
      hours: 24,
      page: 1,
      pageSize: 25,
      protocol: 'all',
      search: 'git'
    });
  });

  it('should request the next page from the API', () => {
    const pagedOverview: ConnectionOverviewDto = {
      ...overview,
      recentConnections: {
        ...overview.recentConnections,
        totalCount: 40,
        totalPages: 2
      }
    };
    api.getOverview.and.returnValue(of(pagedOverview));

    facade.load();
    facade.goToNextPage();

    expect(api.getOverview.calls.mostRecent().args[0]).toEqual({
      hours: 24,
      page: 2,
      pageSize: 25,
      protocol: 'all',
      search: ''
    });
  });
});
