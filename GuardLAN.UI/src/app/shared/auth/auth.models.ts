export interface AuthSessionDto {
  readonly authenticated: boolean;
  readonly username: string | null;
  readonly expiresUtc: string | null;
}

export interface LoginRequestDto {
  readonly username: string;
  readonly password: string;
}
