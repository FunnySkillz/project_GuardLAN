import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AlertDto } from '../../../shared/models/security-alert';
import { AlertDetailDto } from '../models/alert-detail';

export interface AlertReviewRequest {
  readonly note: string | null;
}

@Injectable({ providedIn: 'root' })
export class AlertsApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/alerts';

  list(): Observable<readonly AlertDto[]> {
    return this.http.get<readonly AlertDto[]>(this.apiBaseUrl);
  }

  detail(alertId: string): Observable<AlertDetailDto> {
    return this.http.get<AlertDetailDto>(`${this.apiBaseUrl}/${alertId}`);
  }

  review(alertId: string, request: AlertReviewRequest): Observable<AlertDto> {
    return this.http.patch<AlertDto>(`${this.apiBaseUrl}/${alertId}/review`, request);
  }

  resolve(alertId: string, request: AlertReviewRequest): Observable<AlertDto> {
    return this.http.patch<AlertDto>(`${this.apiBaseUrl}/${alertId}/resolve`, request);
  }

  markFalsePositive(alertId: string, request: AlertReviewRequest): Observable<AlertDto> {
    return this.http.patch<AlertDto>(`${this.apiBaseUrl}/${alertId}/false-positive`, request);
  }

  suppress(alertId: string, request: AlertReviewRequest): Observable<AlertDto> {
    return this.http.patch<AlertDto>(`${this.apiBaseUrl}/${alertId}/suppress`, request);
  }

  reopen(alertId: string, request: AlertReviewRequest): Observable<AlertDto> {
    return this.http.patch<AlertDto>(`${this.apiBaseUrl}/${alertId}/reopen`, request);
  }
}
