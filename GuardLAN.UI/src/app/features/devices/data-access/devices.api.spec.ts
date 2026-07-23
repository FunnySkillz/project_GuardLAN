import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { DevicesApi } from './devices.api';
import { DeviceDto } from '../../../shared/models/network-device';
import { DeviceEvidenceDto } from '../models/device-evidence';

describe('DevicesApi', () => {
  const device: DeviceDto = {
    id: 'device-1',
    ipAddress: '192.168.1.22',
    macAddress: '02:00:00:00:00:22',
    hostname: 'desktop',
    vendor: 'Intel',
    deviceType: 'Desktop',
    isTrusted: true,
    firstSeenUtc: '2026-07-23T10:00:00Z',
    lastSeenUtc: '2026-07-23T11:00:00Z',
    isOnline: true,
    risk: {
      level: 'Normal',
      score: 0,
      reasons: ['No recent risk evidence.']
    }
  };
  const evidence: DeviceEvidenceDto = {
    device,
    summary: {
      sinceUtc: '2026-07-23T10:00:00Z',
      alerts: 1,
      openAlerts: 1,
      dnsQueries: 2,
      blockedDnsQueries: 1,
      uniqueDomains: 2,
      connections: 1,
      uniqueDestinations: 1,
      bytesSent: 100,
      bytesReceived: 200
    },
    recentAlerts: [],
    recentDnsQueries: [],
    recentConnections: []
  };

  let api: DevicesApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), DevicesApi]
    });

    api = TestBed.inject(DevicesApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should request the device inventory', () => {
    api.list().subscribe((devices) => {
      expect(devices).toEqual([device]);
    });

    const request = http.expectOne('/api/devices');
    expect(request.request.method).toBe('GET');
    request.flush([device]);
  });

  it('should patch device changes', () => {
    const changes = { hostname: 'workstation', deviceType: 'Desktop' as const, isTrusted: true };

    api.update(device.id, changes).subscribe((updatedDevice) => {
      expect(updatedDevice.hostname).toBe('workstation');
    });

    const request = http.expectOne(`/api/devices/${device.id}`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual(changes);
    request.flush({ ...device, hostname: 'workstation' });
  });

  it('should request device evidence', () => {
    api.evidence(device.id).subscribe((result) => {
      expect(result).toEqual(evidence);
    });

    const request = http.expectOne(`/api/devices/${device.id}/evidence`);
    expect(request.request.method).toBe('GET');
    request.flush(evidence);
  });
});
