import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render primary navigation', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const navText = compiled.querySelector('.nav-list')?.textContent ?? '';

    expect(compiled.querySelector('.brand-name')?.textContent).toContain('GuardLAN');
    expect(navText).toContain('Dashboard');
    expect(navText).toContain('Devices');
    expect(navText).toContain('DNS');
    expect(navText).toContain('Alerts');
  });
});
