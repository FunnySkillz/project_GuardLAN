import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ConnectionOverviewDto } from '../models/connection-overview';

@Injectable({ providedIn: 'root' })
export class ConnectionsApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/connections';

  getOverview(hours = 24, limit = 200): Observable<ConnectionOverviewDto> {
    return this.http.get<ConnectionOverviewDto>(`${this.apiBaseUrl}/overview`, {
      params: {
        hours: hours.toString(),
        limit: limit.toString()
      }
    });
  }
}
