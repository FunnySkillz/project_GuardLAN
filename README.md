# GuardLAN

GuardLAN is a home/network monitoring dashboard for the local LAN. The plan is strongest if the app treats packet capture and IDS data as external sources, then turns that structured telemetry into device inventory, alerts, and dashboards.

## Plan Analysis

The proposed stack is a good fit:

- Angular 20 for the operational dashboard.
- ASP.NET Core 10 Web API for device, alert, DNS, and connection endpoints.
- PostgreSQL for shared API and worker state.
- A background worker for scan orchestration and later log ingestion.
- Docker Compose for local infrastructure.

The important constraint is also correct: the web app cannot observe the network by itself. Phase 1 should start with inventory and scanner state, then later add Pi-hole, Zeek, Suricata, and notifications as separate ingestion paths.

## First Slice

This init establishes the Phase 1 foundation:

- `GuardLAN.API` contains the ASP.NET Core API, application layer, domain layer, infrastructure layer, worker, backend docs, and Docker Compose.
- `GuardLAN.UI` contains the Angular dashboard shell and frontend docs.

## Architecture

Backend endpoint work must follow [GuardLAN.API/docs/BACKEND_ARCHITECTURE.md](GuardLAN.API/docs/BACKEND_ARCHITECTURE.md).

Angular frontend work must follow [GuardLAN.UI/docs/FRONTEND_ARCHITECTURE.md](GuardLAN.UI/docs/FRONTEND_ARCHITECTURE.md).

## Run Locally

Start PostgreSQL:

```powershell
cd GuardLAN.API
docker compose up -d postgres
```

Run the API:

```powershell
dotnet run --project GuardLAN.API/src/GuardLan.Api
```

Run the Angular app:

```powershell
cd GuardLAN.UI
npm start
```

Useful API endpoints:

```text
GET  http://localhost:5232/api/health
GET  http://localhost:5232/api/dashboard
GET  http://localhost:5232/api/devices
GET  http://localhost:5232/api/alerts
POST http://localhost:5232/api/scans
```
