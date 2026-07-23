import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AlertsApi } from './alerts.api';
import { AlertDto } from '../../../shared/models/security-alert';

describe('AlertsApi', () => {
  const alert: AlertDto = {
    id: 'alert-1',
    deviceId: 'device-1',
    deviceName: 'desktop',
    deviceIpAddress: '192.168.1.22',
    deviceMacAddress: '02:00:00:00:00:22',
    severity: 'High',
    type: 'UnknownDeviceConnected',
    message: 'New unknown device connected at 192.168.1.22.',
    createdUtc: '2026-07-23T10:00:00Z',
    resolvedUtc: null
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

  it('should resolve an alert', () => {
    const resolvedAlert: AlertDto = {
      ...alert,
      resolvedUtc: '2026-07-23T10:30:00Z'
    };

    api.resolve(alert.id).subscribe((response) => {
      expect(response).toEqual(resolvedAlert);
    });

    const request = http.expectOne(`/api/alerts/${alert.id}/resolve`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toBeNull();
    request.flush(resolvedAlert);
  });
});
