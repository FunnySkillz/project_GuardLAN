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
          bytesSent: 1024,
          bytesReceived: 2048,
          firstSeenUtc: '2026-07-23T10:00:00Z',
          lastSeenUtc: '2026-07-23T10:05:00Z'
        }
      ],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1
    }
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
        candidate.params.get('page') === '1' &&
        candidate.params.get('pageSize') === '25' &&
        !candidate.params.has('protocol') &&
        !candidate.params.has('search')
    );
    expect(request.request.method).toBe('GET');
    request.flush(overview);
  });

  it('should request filtered connection overview data', () => {
    api
      .getOverview({
        hours: 12,
        page: 2,
        pageSize: 50,
        protocol: 'udp',
        search: 'dns'
      })
      .subscribe((response) => {
        expect(response).toEqual(overview);
      });

    const request = http.expectOne(
      (candidate) =>
        candidate.url === '/api/connections/overview' &&
        candidate.params.get('hours') === '12' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '50' &&
        candidate.params.get('protocol') === 'udp' &&
        candidate.params.get('search') === 'dns'
    );
    expect(request.request.method).toBe('GET');
    request.flush(overview);
  });
});
