import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ConnectionsApi } from './connections.api';
import { ConnectionOverviewDto } from '../models/connection-overview';

describe('ConnectionsApi', () => {
  const overview: ConnectionOverviewDto = {
    summary: {
      totalConnections: 1,
      activeDevices: 1,
      uniqueDestinations: 1,
      totalBytesSent: 1024,
      totalBytesReceived: 2048
    },
    topProtocols: [{ protocol: 'TCP', connections: 1, bytesSent: 1024, bytesReceived: 2048 }],
    topDestinations: [
      {
        destination: 'github.com',
        destinationIp: '140.82.112.4',
        connections: 1,
        bytesSent: 1024,
        bytesReceived: 2048
      }
    ],
    topDevices: [
      {
        deviceId: 'device-1',
        deviceName: 'desktop',
        deviceIp: '192.168.1.22',
        connections: 1,
        bytesSent: 1024,
        bytesReceived: 2048
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
        bytesSent: 1024,
        bytesReceived: 2048,
        firstSeenUtc: '2026-07-23T10:00:00Z',
        lastSeenUtc: '2026-07-23T10:05:00Z'
      }
    ]
  };

  let api: ConnectionsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ConnectionsApi]
    });

    api = TestBed.inject(ConnectionsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should request the connection overview', () => {
    api.getOverview().subscribe((response) => {
      expect(response).toEqual(overview);
    });

    const request = http.expectOne(
      (candidate) =>
        candidate.url === '/api/connections/overview' &&
        candidate.params.get('hours') === '24' &&
        candidate.params.get('limit') === '200'
    );
    expect(request.request.method).toBe('GET');
    request.flush(overview);
  });
});
