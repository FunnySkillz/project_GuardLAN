# GuardLAN Docker Setup

This document describes the repository-root Docker workflow for running GuardLAN locally with the Angular UI, ASP.NET Core API, scanner worker and PostgreSQL.

The stack is defined from the repository root so one Compose file controls all four services.

## Repository Layout

```text
Project_GuardLAN/
|-- compose.yml
|-- .env.example
|-- GuardLAN.API/
|   |-- Dockerfile
|   |-- Dockerfile.worker
|   |-- .dockerignore
|   `-- src/
|-- GuardLAN.UI/
|   |-- Dockerfile
|   |-- .dockerignore
|   |-- nginx.conf
|   `-- src/
`-- docs/
    `-- Docker.md
```

## Prerequisites

- Docker Desktop or Docker Engine
- Docker Compose v2
- Git

## Quick Start

From the repository root:

```bash
docker compose up --build
```

This starts:

- UI: http://localhost:4200
- API: http://localhost:5232
- Worker: background scanner and ingestion service
- PostgreSQL: localhost:5432

## Environment

Development defaults are provided in [.env.example](../.env.example). Copy it when you want local overrides:

```bash
cp .env.example .env
```

PowerShell:

```powershell
Copy-Item .env.example .env
```

Do not commit a real `.env` file with private credentials. Change the default admin password and internal publisher key before using GuardLAN beyond local development.

Useful variables:

```dotenv
POSTGRES_DB=guardlan
POSTGRES_USER=guardlan
POSTGRES_PASSWORD=guardlan
POSTGRES_HOST_PORT=5432
API_HOST_PORT=5232
UI_HOST_PORT=4200
ASPNETCORE_ENVIRONMENT=Development
GuardLanAuth__AdminUsername=guardlan
GuardLanAuth__AdminPassword=guardlan
GuardLanAuth__InternalPublisherKey=guardlan-dev-internal
LiveUpdates__Http__Enabled=true
LiveUpdates__Http__EndpointUrl=http://api:8080/api/internal/live-updates
```

## Services

### Database

The `database` service uses `postgres:17-alpine` and stores data in the named Docker volume `guardlan-postgres-data`.

Inside Docker, the API reaches PostgreSQL through:

```text
database:5432
```

The host port defaults to `5432` for local database tools.

### API

The `api` service builds from [GuardLAN.API/Dockerfile](../GuardLAN.API/Dockerfile).

It listens on port `8080` inside the container and is exposed on `http://localhost:5232` by default.

Health endpoints:

```text
http://localhost:5232/health
http://localhost:5232/api/health
```

In the current development startup path, the API uses EF Core `EnsureCreated` and seeds development data when the database is reachable. Formal migration tooling is still a future hardening task.

### Worker

The `worker` service builds from [GuardLAN.API/Dockerfile.worker](../GuardLAN.API/Dockerfile.worker).

It processes queued scans, scheduled DNS ingestion, scheduled Zeek imports and scheduled Suricata imports. The worker image includes `nmap` for active discovery.

The worker publishes live updates by calling the API's internal live-update relay endpoint:

```text
http://api:8080/api/internal/live-updates
```

### UI

The `ui` service builds from [GuardLAN.UI/Dockerfile](../GuardLAN.UI/Dockerfile) and serves the production Angular build through Nginx.

The browser calls relative `/api` and `/hubs` paths. Nginx proxies those requests to the internal `api` service, so browser code never needs to resolve Docker-only hostnames.

## Useful Commands

Start the stack:

```bash
docker compose up --build
```

Start in the background:

```bash
docker compose up -d --build
```

Show service status:

```bash
docker compose ps
```

View logs:

```bash
docker compose logs -f
docker compose logs -f api
docker compose logs -f worker
docker compose logs -f ui
docker compose logs -f database
```

Stop the stack while keeping database data:

```bash
docker compose down
```

Reset the local database volume:

```bash
docker compose down --volumes
```

That reset command permanently removes local PostgreSQL data stored by this Compose project.

## Validation

Check Compose syntax:

```bash
docker compose config
```

Build images:

```bash
docker compose build
```

Start services:

```bash
docker compose up -d
```

Confirm health:

```bash
docker compose ps
```

PowerShell API check:

```powershell
Invoke-RestMethod http://localhost:5232/health
```

## Troubleshooting

If a port is already in use, change the corresponding value in `.env`:

```dotenv
UI_HOST_PORT=4201
API_HOST_PORT=5233
POSTGRES_HOST_PORT=5433
```

If the API cannot connect to PostgreSQL, check:

```bash
docker compose ps
docker compose logs database
docker compose logs api
docker compose logs worker
```

If the UI cannot reach the API, check that:

- the API service is healthy
- Nginx is proxying `/api` requests
- the browser network tab shows requests to `localhost`, not the Docker hostname `api`

If live updates do not connect, check that:

- Nginx is proxying `/hubs` requests
- the API service is healthy
- the worker has `LiveUpdates__Http__EndpointUrl=http://api:8080/api/internal/live-updates`
- the API and worker share the same `GuardLanAuth__InternalPublisherKey`

## Production Notes

This Compose setup is for local development and first-run evaluation. Production deployment still needs managed secrets, HTTPS termination, backups, telemetry retention, monitoring and stricter network exposure rules. See [Security hardening](SECURITY_HARDENING.md).
