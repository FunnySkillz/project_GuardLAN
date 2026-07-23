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
        lastSuccessUtc: '2026-07-23T12:00:00Z',
        lastFailureUtc: null,
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
});
