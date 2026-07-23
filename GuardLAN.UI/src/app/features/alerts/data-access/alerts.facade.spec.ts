import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AlertsApi } from './alerts.api';
import { AlertsFacade } from './alerts.facade';
import { AlertDto } from '../../../shared/models/security-alert';

describe('AlertsFacade', () => {
  const highAlert: AlertDto = {
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
  const resolvedAlert: AlertDto = {
    id: 'alert-2',
    deviceId: null,
    deviceName: null,
    deviceIpAddress: null,
    deviceMacAddress: null,
    connectionId: null,
    source: null,
    sourceRecordId: null,
    sourceIp: null,
    destinationIp: null,
    destinationPort: null,
    protocol: null,
    severity: 'Low',
    reviewStatus: 'Resolved',
    type: 'DeviceDisappeared',
    message: 'Device disappeared from the network.',
    createdUtc: '2026-07-23T09:00:00Z',
    reviewedUtc: '2026-07-23T09:30:00Z',
    resolvedUtc: '2026-07-23T09:30:00Z',
    reviewNote: null,
    evidenceSummary: null,
    history: []
  };

  let api: jasmine.SpyObj<
    Pick<AlertsApi, 'list' | 'review' | 'resolve' | 'markFalsePositive' | 'suppress' | 'reopen'>
  >;
  let facade: AlertsFacade;

  beforeEach(() => {
    api = jasmine.createSpyObj<
      Pick<AlertsApi, 'list' | 'review' | 'resolve' | 'markFalsePositive' | 'suppress' | 'reopen'>
    >('AlertsApi', ['list', 'review', 'resolve', 'markFalsePositive', 'suppress', 'reopen']);

    TestBed.configureTestingModule({
      providers: [AlertsFacade, { provide: AlertsApi, useValue: api }]
    });

    facade = TestBed.inject(AlertsFacade);
  });

  it('should load and summarize alerts', () => {
    api.list.and.returnValue(of([highAlert, resolvedAlert]));

    facade.load();

    expect(facade.alerts()).toEqual([highAlert, resolvedAlert]);
    expect(facade.summary()).toEqual({
      total: 2,
      open: 1,
      high: 1,
      reviewed: 0,
      falsePositive: 0,
      suppressed: 0,
      closed: 1
    });
  });

  it('should default to open alerts', () => {
    api.list.and.returnValue(of([highAlert, resolvedAlert]));

    facade.load();

    expect(facade.filteredAlerts()).toEqual([highAlert]);
  });

  it('should filter resolved alerts', () => {
    api.list.and.returnValue(of([highAlert, resolvedAlert]));

    facade.load();
    facade.setFilter('resolved');

    expect(facade.filteredAlerts()).toEqual([resolvedAlert]);
  });

  it('should replace an alert after resolving it', () => {
    const newlyResolvedAlert: AlertDto = {
      ...highAlert,
      reviewStatus: 'Resolved',
      reviewedUtc: '2026-07-23T10:30:00Z',
      resolvedUtc: '2026-07-23T10:30:00Z'
    };

    api.list.and.returnValue(of([highAlert]));
    api.resolve.and.returnValue(of(newlyResolvedAlert));

    facade.load();
    facade.setNote(highAlert.id, 'Fixed');
    facade.submitAction(highAlert.id, 'resolve');

    expect(api.resolve).toHaveBeenCalledWith(highAlert.id, { note: 'Fixed' });
    expect(facade.alerts()).toEqual([newlyResolvedAlert]);
    expect(facade.summary().open).toBe(0);
  });

  it('should filter false-positive alerts', () => {
    const falsePositiveAlert: AlertDto = {
      ...resolvedAlert,
      id: 'alert-3',
      reviewStatus: 'FalsePositive'
    };
    api.list.and.returnValue(of([highAlert, falsePositiveAlert]));

    facade.load();
    facade.setFilter('falsePositive');

    expect(facade.filteredAlerts()).toEqual([falsePositiveAlert]);
  });
});
