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
