import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { IntegrationHealthOverviewDto } from '../models/integration-health';

@Injectable({ providedIn: 'root' })
export class IntegrationsApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/integrations';

  health(): Observable<IntegrationHealthOverviewDto> {
    return this.http.get<IntegrationHealthOverviewDto>(`${this.apiBaseUrl}/health`);
  }
}
