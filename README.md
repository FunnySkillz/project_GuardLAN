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

- `GuardLan.Api` exposes health, dashboard, device, alert, and scan endpoints.
- `GuardLan.Application` contains the first service contracts and DTOs.
- `GuardLan.Domain` contains the core device, connection, DNS, alert, and scan entities.
- `GuardLan.Infrastructure` contains PostgreSQL EF Core persistence and development seed data.
- `GuardLan.Worker` is ready for the nmap scanner loop.
- `GuardLan.Web` is an Angular dashboard shell shaped around the Phase 1 user experience.

## Run Locally

Start PostgreSQL:

```powershell
docker compose up -d postgres
```

Run the API:

```powershell
dotnet run --project src/GuardLan.Api
```

Run the Angular app:

```powershell
cd src/GuardLan.Web
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
