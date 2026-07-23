import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { DnsApi } from './dns.api';
import { DnsOverviewDto } from '../models/dns-overview';

describe('DnsApi', () => {
  const overview: DnsOverviewDto = {
    summary: {
      totalQueries: 1,
      allowedQueries: 1,
      blockedQueries: 0,
      uniqueDomains: 1,
      activeClients: 1
    },
    topDomains: [{ domain: 'github.com', requests: 1, blockedRequests: 0 }],
    topClients: [
      {
        deviceId: 'device-1',
        deviceName: 'desktop',
        clientIp: '192.168.1.22',
        requests: 1,
        blockedRequests: 0
      }
    ],
    recentQueries: [
      {
        id: 'query-1',
        deviceId: 'device-1',
        deviceName: 'desktop',
        clientIp: '192.168.1.22',
        domain: 'github.com',
        wasBlocked: false,
        timestampUtc: '2026-07-23T10:00:00Z'
      }
    ]
  };

  let api: DnsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), DnsApi]
    });

    api = TestBed.inject(DnsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should request the DNS overview', () => {
    api.getOverview().subscribe((response) => {
      expect(response).toEqual(overview);
    });

    const request = http.expectOne(
      (candidate) =>
        candidate.url === '/api/dns/overview' &&
        candidate.params.get('hours') === '24' &&
        candidate.params.get('limit') === '200'
    );
    expect(request.request.method).toBe('GET');
    request.flush(overview);
  });
});
