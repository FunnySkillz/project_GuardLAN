import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DeviceDto, UpdateDeviceRequest } from '../../../shared/models/network-device';

@Injectable({ providedIn: 'root' })
export class DevicesApi {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api/devices';

  list(): Observable<readonly DeviceDto[]> {
    return this.http.get<readonly DeviceDto[]>(this.apiBaseUrl);
  }

  update(deviceId: string, request: UpdateDeviceRequest): Observable<DeviceDto> {
    return this.http.patch<DeviceDto>(`${this.apiBaseUrl}/${deviceId}`, request);
  }
}
