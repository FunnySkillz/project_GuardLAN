import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { App } from './app';
import { DashboardFacade } from './features/dashboard/data-access/dashboard.facade';
import { DashboardOverviewDto } from './features/dashboard/models/dashboard-overview';

describe('App', () => {
  const overview: DashboardOverviewDto = {
    summary: {
      onlineDevices: 1,
      unknownDevices: 0,
      newDevicesToday: 0,
      trustedDevices: 1,
      dnsRequestsToday: 12,
      blockedDomainsToday: 2,
      openAlerts: 0,
      criticalAlerts: 0,
      mostActiveDevices: [],
      mostContactedExternalDomains: [],
      recentAlerts: []
    },
    devices: [],
    recentScans: []
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: DashboardFacade,
          useValue: {
            data: signal(overview),
            devices: signal(overview.devices),
            alerts: signal(overview.summary.recentAlerts),
            domains: signal(overview.summary.mostContactedExternalDomains),
            scans: signal(overview.recentScans),
            loading: signal(false),
            error: signal<string | null>(null),
            queueingScan: signal(false),
            load: jasmine.createSpy('load'),
            queueScan: jasmine.createSpy('queueScan')
          }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand-name')?.textContent).toContain('GuardLAN');
  });
});
