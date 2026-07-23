# GuardLAN Security Hardening

GuardLAN collects sensitive network telemetry. Treat the UI, API, database and integration credentials as private infrastructure.

## Local Authentication

The API uses cookie authentication with a single configured local admin user.

Development defaults:

```text
Username: guardlan
Password: guardlan
```

Set these values through environment variables or `.env` for Docker:

```dotenv
GuardLanAuth__AdminUsername=guardlan
GuardLanAuth__AdminPassword=<strong local password>
GuardLanAuth__InternalPublisherKey=<strong random relay key>
```

The API exposes:

```text
GET  /api/auth/session
POST /api/auth/login
POST /api/auth/logout
```

All regular API controllers and the SignalR hub require an authenticated session. Health endpoints remain anonymous so Docker and local tooling can check service availability.

## Sessions

Session cookies are:

* HTTP-only
* SameSite `Strict`
* Sliding, with `GuardLanAuth__SessionHours` controlling the lifetime

Set secure cookies when GuardLAN runs behind HTTPS:

```dotenv
GuardLanAuth__RequireSecureCookies=true
HttpsRedirection__Enabled=true
```

## Live Update Relay

Browsers receive live updates through the authenticated SignalR hub:

```text
/hubs/guardlan
```

The worker does not publish directly to the hub. It calls:

```text
POST /api/internal/live-updates
```

The API accepts that call only when `X-GuardLAN-Internal-Key` matches `GuardLanAuth__InternalPublisherKey`.

Use a long random value for the internal publisher key and keep the API and worker values synchronized.

## Secret Handling

Do not commit real secrets.

Use `.env`, host environment variables, Docker secrets or a deployment secret store for:

* `GuardLanAuth__AdminPassword`
* `GuardLanAuth__InternalPublisherKey`
* `POSTGRES_PASSWORD`
* `ConnectionStrings__GuardLanDb`
* Pi-hole application passwords
* Future integration API keys

Rotate the admin password and internal publisher key if a `.env` file is shared accidentally.

## HTTPS And Exposure

Keep GuardLAN on a trusted local network unless a proper reverse proxy, HTTPS certificate and authentication policy are in place.

Recommended production posture:

* Terminate HTTPS at a reverse proxy.
* Set `GuardLanAuth__RequireSecureCookies=true`.
* Set `HttpsRedirection__Enabled=true` when the API receives HTTPS directly.
* Restrict inbound access to trusted management devices or VPN clients.
* Do not expose PostgreSQL to the internet.
* Keep sensor log paths and credentials readable only by the service account that needs them.

## Backup

Create a PostgreSQL backup from the Docker stack:

```powershell
docker compose exec -T database pg_dump -U guardlan -d guardlan --clean --if-exists > guardlan-backup.sql
```

Store backups somewhere encrypted if they contain DNS history, connection metadata or IDS alerts.

## Restore

Restore a backup into the running local database:

```powershell
Get-Content .\guardlan-backup.sql | docker compose exec -T database psql -U guardlan -d guardlan
```

For a clean local restore, stop the stack and remove the database volume first:

```powershell
docker compose down --volumes
docker compose up -d database
Get-Content .\guardlan-backup.sql | docker compose exec -T database psql -U guardlan -d guardlan
docker compose up -d
```

The volume reset permanently removes local PostgreSQL data.

## Remaining Hardening

Future production work should add:

* Persistent multi-user accounts or an external identity provider.
* Role-based authorization.
* EF Core migrations instead of development `EnsureCreated`.
* Structured audit logging for login and administrative actions.
* Retention policies for DNS, connection, TLS and alert data.
