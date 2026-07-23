import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ConnectionOverviewDto, ConnectionOverviewQuery } from '../models/connection-overview';

@Injectable({ providedIn: 'root' })
export class ConnectionsApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/connections';

  getOverview(query: ConnectionOverviewQuery = defaultConnectionOverviewQuery): Observable<ConnectionOverviewDto> {
    const params: Record<string, string> = {
      hours: query.hours.toString(),
      page: query.page.toString(),
      pageSize: query.pageSize.toString()
    };

    if (query.protocol !== 'all') {
      params['protocol'] = query.protocol;
    }

    if (query.search.trim()) {
      params['search'] = query.search.trim();
    }

    return this.http.get<ConnectionOverviewDto>(`${this.apiBaseUrl}/overview`, {
      params
    });
  }
}

export const defaultConnectionOverviewQuery: ConnectionOverviewQuery = {
  hours: 24,
  page: 1,
  pageSize: 25,
  protocol: 'all',
  search: ''
};
