import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DnsOverviewDto } from '../models/dns-overview';

@Injectable({ providedIn: 'root' })
export class DnsApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/dns';

  getOverview(hours = 24, limit = 200): Observable<DnsOverviewDto> {
    return this.http.get<DnsOverviewDto>(`${this.apiBaseUrl}/overview`, {
      params: {
        hours: hours.toString(),
        limit: limit.toString()
      }
    });
  }
}
