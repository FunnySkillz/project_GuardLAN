import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { DnsApi } from './dns.api';
import { DnsFacade } from './dns.facade';
import { DnsOverviewDto } from '../models/dns-overview';

describe('DnsFacade', () => {
  const overview: DnsOverviewDto = {
    summary: {
      totalQueries: 2,
      allowedQueries: 1,
      blockedQueries: 1,
      uniqueDomains: 2,
      activeClients: 1
    },
    topDomains: [
      { domain: 'github.com', requests: 1, blockedRequests: 0 },
      { domain: 'ads.example', requests: 1, blockedRequests: 1 }
    ],
    topClients: [
      {
        deviceId: 'device-1',
        deviceName: 'desktop',
        clientIp: '192.168.1.22',
        requests: 2,
        blockedRequests: 1
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
      },
      {
        id: 'query-2',
        deviceId: 'device-1',
        deviceName: 'desktop',
        clientIp: '192.168.1.22',
        domain: 'ads.example',
        wasBlocked: true,
        timestampUtc: '2026-07-23T10:01:00Z'
      }
    ]
  };

  let api: jasmine.SpyObj<Pick<DnsApi, 'getOverview'>>;
  let facade: DnsFacade;

  beforeEach(() => {
    api = jasmine.createSpyObj<Pick<DnsApi, 'getOverview'>>('DnsApi', ['getOverview']);

    TestBed.configureTestingModule({
      providers: [DnsFacade, { provide: DnsApi, useValue: api }]
    });

    facade = TestBed.inject(DnsFacade);
  });

  it('should load DNS overview data', () => {
    api.getOverview.and.returnValue(of(overview));

    facade.load();

    expect(facade.summary()).toEqual(overview.summary);
    expect(facade.topDomains()).toEqual(overview.topDomains);
    expect(facade.filteredQueries()).toEqual(overview.recentQueries);
  });

  it('should filter blocked DNS queries', () => {
    api.getOverview.and.returnValue(of(overview));

    facade.load();
    facade.setFilter('blocked');

    expect(facade.filteredQueries()).toEqual([overview.recentQueries[1]]);
  });

  it('should search DNS queries by domain or device', () => {
    api.getOverview.and.returnValue(of(overview));

    facade.load();
    facade.setSearch('git');

    expect(facade.filteredQueries()).toEqual([overview.recentQueries[0]]);
  });
});
