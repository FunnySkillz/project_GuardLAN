import { ConnectionDto } from '../../connections/models/connection-overview';
import { AlertDto } from '../../../shared/models/security-alert';

export interface AlertDetailDto {
  readonly alert: AlertDto;
  readonly relatedConnection: ConnectionDto | null;
}
