import { Routes } from '@angular/router';

import { authGuard } from './shared/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login-page.component').then((module) => module.LoginPageComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/dashboard/ui/dashboard-page.component').then(
            (module) => module.DashboardPageComponent
          )
      },
      {
        path: 'devices/:id',
        loadComponent: () =>
          import('./features/devices/ui/device-evidence-page.component').then(
            (module) => module.DeviceEvidencePageComponent
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
        path: 'alerts/:id',
        loadComponent: () =>
          import('./features/alerts/ui/alert-detail-page.component').then(
            (module) => module.AlertDetailPageComponent
          )
      },
      {
        path: 'alerts',
        loadComponent: () =>
          import('./features/alerts/ui/alerts-page.component').then(
            (module) => module.AlertsPageComponent
          )
      },
      {
        path: 'integrations',
        loadComponent: () =>
          import('./features/integrations/ui/integrations-page.component').then(
            (module) => module.IntegrationsPageComponent
          )
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
