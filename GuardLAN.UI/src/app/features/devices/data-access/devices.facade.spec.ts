import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { DevicesApi } from './devices.api';
import { DevicesFacade } from './devices.facade';
import { DeviceDto } from '../../../shared/models/network-device';

describe('DevicesFacade', () => {
  const trustedDevice: DeviceDto = {
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
  const reviewDevice: DeviceDto = {
    id: 'device-2',
    ipAddress: '192.168.1.71',
    macAddress: '02:00:00:00:00:71',
    hostname: null,
    vendor: null,
    deviceType: 'Unknown',
    isTrusted: false,
    firstSeenUtc: '2026-07-23T10:30:00Z',
    lastSeenUtc: '2026-07-23T10:45:00Z',
    isOnline: false,
    risk: {
      level: 'Medium',
      score: 35,
      reasons: ['1 open medium-severity alert.']
    }
  };

  let api: jasmine.SpyObj<Pick<DevicesApi, 'list' | 'update'>>;
  let facade: DevicesFacade;

  beforeEach(() => {
    api = jasmine.createSpyObj<Pick<DevicesApi, 'list' | 'update'>>('DevicesApi', [
      'list',
      'update'
    ]);

    TestBed.configureTestingModule({
      providers: [DevicesFacade, { provide: DevicesApi, useValue: api }]
    });

    facade = TestBed.inject(DevicesFacade);
  });

  it('should load and summarize devices', () => {
    api.list.and.returnValue(of([trustedDevice, reviewDevice]));

    facade.load();

    expect(facade.devices()).toEqual([trustedDevice, reviewDevice]);
    expect(facade.summary()).toEqual({
      total: 2,
      online: 1,
      review: 1,
      risk: 1,
      offline: 1,
      trusted: 1
    });
  });

  it('should filter devices that need review', () => {
    api.list.and.returnValue(of([trustedDevice, reviewDevice]));

    facade.load();
    facade.setFilter('review');

    expect(facade.filteredDevices()).toEqual([reviewDevice]);
  });

  it('should filter devices with elevated risk evidence', () => {
    api.list.and.returnValue(of([trustedDevice, reviewDevice]));

    facade.load();
    facade.setFilter('risk');

    expect(facade.filteredDevices()).toEqual([reviewDevice]);
  });

  it('should replace a device after saving changes', () => {
    const updatedDevice: DeviceDto = {
      ...reviewDevice,
      hostname: 'printer',
      deviceType: 'Printer',
      isTrusted: true
    };

    api.list.and.returnValue(of([reviewDevice]));
    api.update.and.returnValue(of(updatedDevice));

    facade.load();
    facade.saveDevice(reviewDevice.id, {
      hostname: 'printer',
      deviceType: 'Printer',
      isTrusted: true
    });

    expect(api.update).toHaveBeenCalledWith(reviewDevice.id, {
      hostname: 'printer',
      deviceType: 'Printer',
      isTrusted: true
    });
    expect(facade.devices()).toEqual([updatedDevice]);
  });
});
