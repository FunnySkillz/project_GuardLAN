import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthSessionDto, LoginRequestDto } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/auth';

  getSession(): Observable<AuthSessionDto> {
    return this.http.get<AuthSessionDto>(`${this.apiBaseUrl}/session`);
  }

  login(request: LoginRequestDto): Observable<AuthSessionDto> {
    return this.http.post<AuthSessionDto>(`${this.apiBaseUrl}/login`, request);
  }

  logout(): Observable<AuthSessionDto> {
    return this.http.post<AuthSessionDto>(`${this.apiBaseUrl}/logout`, {});
  }
}
