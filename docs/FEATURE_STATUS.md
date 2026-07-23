# GuardLAN Feature Status

## Purpose

This document is the primary project-level tracker for GuardLAN features and technical capabilities. It is based on the repository implementation rather than on roadmap text alone.

## Status Definitions

| Status | Meaning |
|---|---|
| Not Started | No meaningful implementation exists yet. |
| Planned | The feature is documented or approved but implementation has not started. |
| In Progress | The feature is actively being implemented. |
| Partially Implemented | Important parts exist, but the feature is not complete or production-usable. |
| Implemented | The intended current scope is implemented and usable. |
| Blocked | Implementation cannot continue because of a documented dependency or unresolved decision. |
| Deprecated | The feature or implementation should no longer be used. |

## Current Project Summary

GuardLAN currently provides the first version of a device-visibility workflow. The application can scan a configured subnet, persist discovered devices and scan history, expose dashboard and alert endpoints, ingest DNS records from a configurable Pi-hole source, expose stored connection metadata, and present device, alert, DNS and connection views in the Angular UI.

DNS visibility now has a first ingestion path through Pi-hole. Network connection telemetry has a stored-data overview flow, but it still needs production ingestion and scalable history paging. Authentication, real-time updates, and most external monitoring integrations remain incomplete.

## Feature Overview

| Feature | Status | Current State | Next Required Change | Main Area |
|---|---|---|---|---|
| Device discovery | Implemented | Nmap-based scanning detects hosts and stores them as devices. | Improve classification and review support for newly discovered devices. | API / UI |
| Device inventory | Implemented | Devices can be listed, viewed and updated through API and UI. | Add stronger review and enrichment workflows. | API / UI |
| Manual network scanning | Implemented | A scan can be queued from the API and triggered from the dashboard UI. | Add scan result detail views and user-facing scan history improvements. | API / UI |
| Scheduled or background scanning | Partially Implemented | A background worker processes queued scans on an interval. | Add a clear scheduling model and scan policy management. | Worker |
| Device trust management | Implemented | Devices can be marked trusted or untrusted through the API and UI. | Add review workflows for suspicious or unknown devices. | API / UI |
| Device classification | Partially Implemented | Device types are modeled and editable, but classification is still manual. | Introduce automatic or semi-automatic classification logic. | API / UI |
| Dashboard summary | Implemented | Dashboard endpoints and UI provide overview metrics, device activity and recent alerts. | Add richer time-based analytics and deeper drill-down views. | API / UI |
| Alert management | Implemented | Alerts are created, listed, and resolved through API and UI. | Add richer correlation, severity handling and alert history. | API / UI |
| DNS monitoring | Partially Implemented | Stored DNS queries are exposed through a DNS overview API and Angular DNS activity page, with a configurable Pi-hole ingestion pipeline for importing DNS history. | Validate the importer against a live Pi-hole instance and add retention plus newly contacted domain detection. | API / Worker / UI |
| Network connection monitoring | Partially Implemented | Connection entities, dashboard aggregation, a connection overview API and an Angular connection activity page exist, but no full telemetry ingestion path is implemented. | Add backend pagination and a normalized Zeek connection ingestion path. | API / Worker / UI |
| Pi-hole integration | Partially Implemented | A configurable Pi-hole query importer, manual API trigger and worker schedule exist, but live-appliance validation and operational diagnostics are still incomplete. | Validate response shapes against Pi-hole's local API docs and add import health reporting. | API / Worker |
| Zeek integration | Planned | No ingestion flow exists yet. | Add a normalized ingestion contract for Zeek metadata. | API / Worker |
| Suricata integration | Planned | No integration exists yet. | Add alert and event ingestion for IDS telemetry. | API / Worker |
| SignalR real-time updates | Planned | No real-time transport is implemented. | Add live updates for dashboards and alerts. | API / UI |
| Authentication and authorization | Not Started | No auth layer exists in the repository. | Introduce user identity and permission rules. | API |
| Docker local development | Partially Implemented | A PostgreSQL compose setup exists, but the full UI/API/database stack is not yet assembled at the repository root. | Add a complete multi-service compose setup and health checks. | Infrastructure |
| PostgreSQL persistence | Implemented | EF Core persistence, repositories and seeded development data are in place. | Add migration tooling and operational backup/retention practices. | Database |
| Mobile Device Activity Collector | Planned | The MDAC design exists in documentation, but no implementation is present. | Create the mobile app and backend ingestion contract. | Mobile |

## Feature Details

### Device Discovery

**Status:** Implemented

**Current State**
- Nmap-based subnet scanning is implemented in [GuardLAN.API/src/GuardLan.Infrastructure/Scanning/NmapNetworkScanner.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Scanning/NmapNetworkScanner.cs).
- Scan execution is orchestrated through [GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs).
- Discovered devices are persisted and updated through the device repository and unit of work layer.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Api/Controllers/ScansController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/ScansController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/NetworkScanService.cs](../GuardLAN.API/src/GuardLan.Application/Services/NetworkScanService.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Scanning/NmapNetworkScanner.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Scanning/NmapNetworkScanner.cs)
- [GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts](../GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts)

**Missing or Incomplete**
- Automated device-type classification
- Better vendor or host identification
- Review workflow for newly discovered devices

**Next Change**
Introduce a device-review workflow that supports naming, classification and trust decisions for newly discovered hosts.

### Device Inventory

**Status:** Implemented

**Current State**
- Device inventory is exposed through the backend device controller and service layer.
- The UI includes a dedicated devices page with filtering, searching and trust updates.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Api/Controllers/DevicesController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/DevicesController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs)
- [GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts)
- [GuardLAN.UI/src/app/features/devices/data-access/devices.facade.ts](../GuardLAN.UI/src/app/features/devices/data-access/devices.facade.ts)

**Missing or Incomplete**
- Device enrichment beyond hostname, type and trust state
- Better review and grouping for unknown or suspicious devices

**Next Change**
Add a richer device-detail experience that combines inventory data with recent alerts, DNS activity and connection history.

### Network Scanning

**Status:** Implemented

**Current State**
- Manual scan queuing is supported through the API and dashboard UI.
- A worker service processes queued scans and updates device state accordingly.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Api/Controllers/ScansController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/ScansController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs)
- [GuardLAN.API/src/GuardLan.Worker/Worker.cs](../GuardLAN.API/src/GuardLan.Worker/Worker.cs)
- [GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts](../GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts)

**Missing or Incomplete**
- A true scheduling model for recurring scans
- Scan result detail pages and richer scan analytics

**Next Change**
Introduce a configurable scan schedule and scan history views that show what changed between runs.

### Device Trust Management

**Status:** Implemented

**Current State**
- Devices can be marked trusted or untrusted and the value is persisted.
- The UI exposes trust state as part of the device inventory experience.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs)
- [GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts)
- [GuardLAN.UI/src/app/shared/models/network-device.ts](../GuardLAN.UI/src/app/shared/models/network-device.ts)

**Missing or Incomplete**
- Trust actions are still manual and not tied to a broader review policy

**Next Change**
Add explicit review and trust policies that combine device metadata with risk context.

### Device Classification

**Status:** Partially Implemented

**Current State**
- Device type is modeled as an enum and can be edited through the API and UI.
- Initial classification values exist, but classification is not automated.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Domain/Enums/DeviceType.cs](../GuardLAN.API/src/GuardLan.Domain/Enums/DeviceType.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs)
- [GuardLAN.UI/src/app/shared/models/network-device.ts](../GuardLAN.UI/src/app/shared/models/network-device.ts)

**Missing or Incomplete**
- Automatic or semi-automatic classification
- Confidence-based classification or vendor-assisted labeling

**Next Change**
Introduce a classification engine that infers device categories from scan metadata and observed behavior.

### Dashboard

**Status:** Implemented

**Current State**
- The backend exposes summary and overview DTOs for the dashboard.
- The Angular dashboard displays devices, alerts, recent scans and top domains.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Api/Controllers/DashboardController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/DashboardController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/DashboardSnapshotDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/DashboardSnapshotDto.cs)
- [GuardLAN.UI/src/app/features/dashboard/data-access/dashboard.facade.ts](../GuardLAN.UI/src/app/features/dashboard/data-access/dashboard.facade.ts)
- [GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts](../GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts)

**Missing or Incomplete**
- Time-series charts and trend views
- Deeper drill-downs for alerts, domains and device behavior

**Next Change**
Add richer analytics widgets and trend-oriented views without overloading the initial dashboard screen.

### Alert Management

**Status:** Implemented

**Current State**
- Security alerts are stored, listed and resolved through API and UI.
- Alert creation is already wired into scan execution for discovered-device and disappearance events.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Api/Controllers/AlertsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/AlertsController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/AlertService.cs](../GuardLAN.API/src/GuardLan.Application/Services/AlertService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs)
- [GuardLAN.UI/src/app/features/alerts/ui/alerts-page.component.ts](../GuardLAN.UI/src/app/features/alerts/ui/alerts-page.component.ts)
- [GuardLAN.UI/src/app/features/alerts/data-access/alerts.facade.ts](../GuardLAN.UI/src/app/features/alerts/data-access/alerts.facade.ts)

**Missing or Incomplete**
- Correlation rules beyond basic event creation
- Alert enrichment, status history and notification workflows

**Next Change**
Add richer alert correlation and severity logic so alerts can be understood as part of a broader network story.

### DNS Monitoring

**Status:** Partially Implemented

**Current State**
- The domain model includes DNS query entities and persistence support.
- The dashboard aggregates DNS query counts and blocked-domain counts from stored data.
- Example DNS data is seeded for development.
- Stored DNS query data is exposed through a dedicated DNS overview API.
- The Angular UI includes a DNS activity page with summary metrics, top domains, top clients, search, filtering and recent query history.
- The backend includes a configurable Pi-hole importer with manual and scheduled ingestion paths.
- Imported DNS records are deduplicated and matched to known devices by client IP.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Domain/Entities/DnsQuery.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/DnsQuery.cs)
- [GuardLAN.API/src/GuardLan.Api/Controllers/DnsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/DnsController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DnsIngestionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DnsIngestionService.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Dns/PiHoleDnsQuerySource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Dns/PiHoleDnsQuerySource.cs)
- [GuardLAN.API/src/GuardLan.Worker/Worker.cs](../GuardLAN.API/src/GuardLan.Worker/Worker.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DnsService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DnsService.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/DnsQueryDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/DnsQueryDto.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/GuardLanDbContext.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/GuardLanDbContext.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/DatabaseSeeder.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/DatabaseSeeder.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs)
- [GuardLAN.UI/src/app/features/dns/ui/dns-page.component.ts](../GuardLAN.UI/src/app/features/dns/ui/dns-page.component.ts)
- [GuardLAN.UI/src/app/features/dns/data-access/dns.facade.ts](../GuardLAN.UI/src/app/features/dns/data-access/dns.facade.ts)

**Missing or Incomplete**
- Validation against a live Pi-hole instance
- Import diagnostics and health reporting
- Unknown or newly contacted domain detection
- Retention cleanup for DNS history

**Next Change**
Validate the Pi-hole ingestion pipeline against a live instance, then add import health reporting, new-domain detection and retention cleanup.

### Network Connection Monitoring

**Status:** Partially Implemented

**Current State**
- Network connection entities, repositories and persistence models exist.
- The dashboard uses connection data to calculate active-device traffic summaries.
- Stored connection metadata is exposed through a dedicated connection overview API.
- The Angular UI includes a connection activity page with summary metrics, protocol filtering, search and recent connection history.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Domain/Entities/NetworkConnection.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/NetworkConnection.cs)
- [GuardLAN.API/src/GuardLan.Api/Controllers/ConnectionsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/ConnectionsController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ConnectionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ConnectionService.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/ConnectionOverviewDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/ConnectionOverviewDto.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/GuardLanDbContext.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/GuardLanDbContext.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs)
- [GuardLAN.UI/src/app/features/connections/ui/connections-page.component.ts](../GuardLAN.UI/src/app/features/connections/ui/connections-page.component.ts)
- [GuardLAN.UI/src/app/features/connections/data-access/connections.facade.ts](../GuardLAN.UI/src/app/features/connections/data-access/connections.facade.ts)

**Missing or Incomplete**
- Production ingestion of connection metadata
- Backend pagination and server-side filtering for large history sets
- Connection detail pages and richer traffic analytics

**Next Change**
Add backend pagination for connection history, then introduce a normalized ingestion contract for Zeek connection telemetry.

### External Security Integrations

**Status:** Partially Implemented

**Current State**
- The repository documents integrations such as Pi-hole, Zeek and Suricata.
- Pi-hole DNS ingestion has a first backend implementation.
- Zeek and Suricata ingestion flows are still planned.

**Implemented In**
- [README.md](../README.md)
- [docs/MDAC/README.md](../docs/MDAC/README.md)
- [docs/PIHOLE.md](PIHOLE.md)
- [GuardLAN.API/src/GuardLan.Infrastructure/Dns/PiHoleDnsQuerySource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Dns/PiHoleDnsQuerySource.cs)

**Missing or Incomplete**
- Live validation for Pi-hole integration
- Zeek and Suricata connector implementations
- Shared event normalization and alert mapping

**Next Change**
Validate the first connector against a live Pi-hole instance, then reuse the ingestion pattern for Zeek and Suricata.

### SignalR Real-Time Updates

**Status:** Planned

**Current State**
- The architecture documentation mentions SignalR for future real-time updates, but no hub or live update flow exists.

**Implemented In**
- [README.md](../README.md)
- [GuardLAN.UI/docs/FRONTEND_ARCHITECTURE.md](../GuardLAN.UI/docs/FRONTEND_ARCHITECTURE.md)

**Missing or Incomplete**
- Event streaming
- Live refresh for dashboard and alerts

**Next Change**
Add a minimal real-time update path for the dashboard or alerts before expanding it further.

### Authentication and Authorization

**Status:** Not Started

**Current State**
- No authentication middleware, user identity layer or authorization rules are present.
- The API currently exposes endpoints without access control.

**Implemented In**
- None.

**Missing or Incomplete**
- User accounts and authentication
- Role-based or permission-based access control

**Next Change**
Introduce a basic authentication model and secure the main API endpoints.

### Docker and Local Deployment

**Status:** Partially Implemented

**Current State**
- A PostgreSQL-only compose setup exists in [GuardLAN.API/docker-compose.yml](../GuardLAN.API/docker-compose.yml).
- The backend and UI are not yet composed together into a single repository-level local stack.

**Implemented In**
- [GuardLAN.API/docker-compose.yml](../GuardLAN.API/docker-compose.yml)
- [GuardLAN.API/src/GuardLan.Api/Program.cs](../GuardLAN.API/src/GuardLan.Api/Program.cs)

**Missing or Incomplete**
- Full UI/API/database orchestration
- Health checks and startup sequencing beyond the database container

**Next Change**
Create a single repository-level compose workflow for UI, API and database with health checks and clear startup instructions.

### Mobile Device Activity Collector

**Status:** Planned

**Current State**
- The repository contains an architecture document for MDAC, but no mobile application or backend implementation is present.

**Implemented In**
- [docs/MDAC/README.md](../docs/MDAC/README.md)

**Missing or Incomplete**
- Mobile client implementation
- Secure synchronization and device-activity ingestion

**Next Change**
Define the first MDAC delivery scope and implement a basic mobile-to-API sync path.

## Current Development Focus

Phase 2: Network Connection Model.

The current phase now exposes stored connection metadata through a backend overview endpoint and Angular page. The next implementation slice should add backend pagination and define the normalized ingestion contract that Zeek will populate.

## Known Technical Gaps

- Authentication and authorization are not implemented.
- Real-time updates are not available.
- External integrations are only partially normalized behind shared ingestion contracts.
- DNS visibility has an API, UI and Pi-hole importer, but the importer still needs live validation and operational diagnostics.
- Connection telemetry has stored-data reporting, but it is not yet backed by a complete ingestion pipeline.
- The local deployment story is only partially containerized.

## Update Rules

This document must be updated whenever:

- A new major feature is introduced
- A feature enters active development
- A feature becomes partially implemented
- A feature becomes implemented
- A feature becomes blocked
- A planned feature is removed or deprecated
- The current development focus changes

When updating a feature:

1. Verify the actual code before changing the status.
2. Update the overview table.
3. Update the detailed feature section.
4. Update the implementation paths.
5. Record the most important next change.
6. Remove completed items from the Missing or Incomplete section.
7. Do not mark a feature as implemented only because scaffolding or placeholder classes exist.
8. Do not use this document for tiny refactoring tasks or individual bugs.
