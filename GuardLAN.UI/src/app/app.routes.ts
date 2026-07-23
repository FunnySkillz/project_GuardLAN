import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/dashboard/ui/dashboard-page.component').then(
        (module) => module.DashboardPageComponent
      )
  },
  {
    path: 'devices',
    loadComponent: () =>
      import('./features/devices/ui/devices-page.component').then(
        (module) => module.DevicesPageComponent
      )
  },
  {
    path: 'connections',
    loadComponent: () =>
      import('./features/connections/ui/connections-page.component').then(
        (module) => module.ConnectionsPageComponent
      )
  },
  {
    path: 'dns',
    loadComponent: () =>
      import('./features/dns/ui/dns-page.component').then((module) => module.DnsPageComponent)
  },
  {
    path: 'alerts',
    loadComponent: () =>
      import('./features/alerts/ui/alerts-page.component').then(
        (module) => module.AlertsPageComponent
      )
  },
  {
    path: '**',
    redirectTo: ''
  }
];
