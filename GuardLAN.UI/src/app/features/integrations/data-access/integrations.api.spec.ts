import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { IntegrationsApi } from './integrations.api';
import { IntegrationHealthOverviewDto } from '../models/integration-health';

describe('IntegrationsApi', () => {
  const overview: IntegrationHealthOverviewDto = {
    summary: {
      totalSources: 1,
      healthySources: 1,
      warningSources: 0,
      unavailableSources: 0,
      disabledSources: 0,
      staleSources: 0,
      lastCheckedUtc: '2026-07-23T12:00:00Z'
    },
    sources: [
      {
        id: 'health-1',
        source: 'Pi-hole',
        kind: 'Dns',
        status: 'Healthy',
        sourceEnabled: true,
        sourceAvailable: true,
        lastCheckedUtc: '2026-07-23T12:00:00Z',
        staleAfterUtc: '2026-07-23T12:15:00Z',
        lastSuccessUtc: '2026-07-23T12:00:00Z',
        lastFailureUtc: null,
        recordsRead: 10,
        recordsImported: 8,
        recordsRejected: 0,
        message: 'Imported 8 DNS records from Pi-hole.'
      }
    ],
    recentRuns: [
      {
        id: 'run-1',
        source: 'Pi-hole',
        kind: 'Dns',
        status: 'Healthy',
        sourceEnabled: true,
        sourceAvailable: true,
        completedUtc: '2026-07-23T12:00:00Z',
        recordsRead: 10,
        recordsImported: 8,
        recordsRejected: 0,
        message: 'Imported 8 DNS records from Pi-hole.'
      }
    ]
  };

  let api: IntegrationsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), IntegrationsApi]
    });

    api = TestBed.inject(IntegrationsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should request integration health', () => {
    api.health().subscribe((result) => {
      expect(result).toEqual(overview);
    });

    const request = http.expectOne('/api/integrations/health');
    expect(request.request.method).toBe('GET');
    request.flush(overview);
  });

  it('should trigger Pi-hole import', () => {
    api.importNow('pihole').subscribe((result) => {
      expect(result).toEqual({});
    });

    const request = http.expectOne('/api/dns/import/pihole');
    expect(request.request.method).toBe('POST');
    request.flush({});
  });

  it('should trigger Zeek import', () => {
    api.importNow('zeek').subscribe((result) => {
      expect(result).toEqual({});
    });

    const request = http.expectOne('/api/integrations/zeek/import');
    expect(request.request.method).toBe('POST');
    request.flush({});
  });

  it('should trigger Suricata import', () => {
    api.importNow('suricata').subscribe((result) => {
      expect(result).toEqual({});
    });

    const request = http.expectOne('/api/integrations/suricata/import');
    expect(request.request.method).toBe('POST');
    request.flush({});
  });
});
