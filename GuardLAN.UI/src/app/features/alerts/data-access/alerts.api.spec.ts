import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AlertsApi } from './alerts.api';
import { AlertDto } from '../../../shared/models/security-alert';
import { AlertDetailDto } from '../models/alert-detail';

describe('AlertsApi', () => {
  const alert: AlertDto = {
    id: 'alert-1',
    deviceId: 'device-1',
    deviceName: 'desktop',
    deviceIpAddress: '192.168.1.22',
    deviceMacAddress: '02:00:00:00:00:22',
    connectionId: null,
    source: null,
    sourceRecordId: null,
    sourceIp: null,
    destinationIp: null,
    destinationPort: null,
    protocol: null,
    severity: 'High',
    reviewStatus: 'Open',
    type: 'UnknownDeviceConnected',
    message: 'New unknown device connected at 192.168.1.22.',
    createdUtc: '2026-07-23T10:00:00Z',
    reviewedUtc: null,
    resolvedUtc: null,
    reviewNote: null,
    evidenceSummary: null,
    history: []
  };

  let api: AlertsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AlertsApi]
    });

    api = TestBed.inject(AlertsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should request alerts', () => {
    api.list().subscribe((alerts) => {
      expect(alerts).toEqual([alert]);
    });

    const request = http.expectOne('/api/alerts');
    expect(request.request.method).toBe('GET');
    request.flush([alert]);
  });

  it('should request alert details', () => {
    const detail: AlertDetailDto = {
      alert,
      relatedConnection: null
    };

    api.detail(alert.id).subscribe((response) => {
      expect(response).toEqual(detail);
    });

    const request = http.expectOne(`/api/alerts/${alert.id}`);
    expect(request.request.method).toBe('GET');
    request.flush(detail);
  });

  it('should resolve an alert', () => {
    const resolvedAlert: AlertDto = {
      ...alert,
      reviewStatus: 'Resolved',
      reviewedUtc: '2026-07-23T10:30:00Z',
      resolvedUtc: '2026-07-23T10:30:00Z',
      reviewNote: 'Fixed'
    };

    api.resolve(alert.id, { note: 'Fixed' }).subscribe((response) => {
      expect(response).toEqual(resolvedAlert);
    });

    const request = http.expectOne(`/api/alerts/${alert.id}/resolve`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ note: 'Fixed' });
    request.flush(resolvedAlert);
  });

  it('should mark an alert false positive', () => {
    const falsePositiveAlert: AlertDto = {
      ...alert,
      reviewStatus: 'FalsePositive',
      reviewedUtc: '2026-07-23T10:30:00Z',
      resolvedUtc: '2026-07-23T10:30:00Z',
      reviewNote: 'Benign test'
    };

    api.markFalsePositive(alert.id, { note: 'Benign test' }).subscribe((response) => {
      expect(response).toEqual(falsePositiveAlert);
    });

    const request = http.expectOne(`/api/alerts/${alert.id}/false-positive`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ note: 'Benign test' });
    request.flush(falsePositiveAlert);
  });
});
