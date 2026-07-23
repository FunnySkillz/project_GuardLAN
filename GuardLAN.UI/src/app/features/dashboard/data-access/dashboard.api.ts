import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DashboardOverviewDto, NetworkScanDto, QueueNetworkScanRequest } from '../models/dashboard-overview';

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api';

  getOverview(): Observable<DashboardOverviewDto> {
    return this.http.get<DashboardOverviewDto>(`${this.apiBaseUrl}/dashboard/overview`);
  }

  queueScan(request: QueueNetworkScanRequest): Observable<NetworkScanDto> {
    return this.http.post<NetworkScanDto>(`${this.apiBaseUrl}/scans`, request);
  }
}
