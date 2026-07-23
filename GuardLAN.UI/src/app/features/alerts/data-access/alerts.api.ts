import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AlertDto } from '../../../shared/models/security-alert';

@Injectable({ providedIn: 'root' })
export class AlertsApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/alerts';

  list(): Observable<readonly AlertDto[]> {
    return this.http.get<readonly AlertDto[]>(this.apiBaseUrl);
  }

  resolve(alertId: string): Observable<AlertDto> {
    return this.http.patch<AlertDto>(`${this.apiBaseUrl}/${alertId}/resolve`, null);
  }
}
