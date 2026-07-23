import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  IntegrationHealthOverviewDto,
  IntegrationImportTarget
} from '../models/integration-health';

@Injectable({ providedIn: 'root' })
export class IntegrationsApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/integrations';

  health(): Observable<IntegrationHealthOverviewDto> {
    return this.http.get<IntegrationHealthOverviewDto>(`${this.apiBaseUrl}/health`);
  }

  importNow(target: IntegrationImportTarget): Observable<unknown> {
    switch (target) {
      case 'pihole':
        return this.http.post('/api/dns/import/pihole', null);
      case 'zeek':
        return this.http.post(`${this.apiBaseUrl}/zeek/import`, null);
      case 'suricata':
        return this.http.post(`${this.apiBaseUrl}/suricata/import`, null);
    }
  }
}
