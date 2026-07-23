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

GuardLAN currently provides the first version of a device-visibility workflow. The application can scan a configured subnet, persist discovered devices and scan history, protect the dashboard with local-user authentication, expose dashboard and alert endpoints, ingest DNS records from a configurable Pi-hole source, accept normalized connection metadata, import Zeek connection/DNS/TLS logs, import Suricata Eve JSON IDS alerts, publish SignalR live updates, record integration health, and present device, alert, DNS, connection and integration views in the Angular UI.

DNS visibility now has a first ingestion path through Pi-hole and Zeek `dns.log`. Network connection telemetry has a stored-data overview flow, dashboard traffic and protocol widgets, a normalized import endpoint, Zeek `conn.log` ingestion and stored TLS observations from Zeek `ssl.log`. Suricata alert ingestion now imports IDS evidence into alert history and associates alerts with devices and connections where possible. SignalR live updates refresh the dashboard, devices, DNS and alerts views after operational changes. The first authentication and hardening slice is implemented. Device inventory responses include explainable risk summaries, device detail pages now show recent alert, DNS and connection evidence, and the Integrations page shows source health, stale state and import history for Pi-hole, Zeek and Suricata. Deeper multi-user identity, roles and audit logging remain future work.

## Feature Overview

| Feature | Status | Current State | Next Required Change | Main Area |
|---|---|---|---|---|
| Device discovery | Implemented | Nmap-based scanning detects hosts and stores them as devices. | Improve classification and review support for newly discovered devices. | API / UI |
| Device inventory | Implemented | Devices can be listed, viewed, updated and opened in a detail evidence page through API and UI. | Add stronger review and enrichment workflows. | API / UI |
| Manual network scanning | Implemented | A scan can be queued from the API and triggered from the dashboard UI. | Add scan result detail views and user-facing scan history improvements. | API / UI |
| Scheduled or background scanning | Partially Implemented | A background worker processes queued scans on an interval. | Add a clear scheduling model and scan policy management. | Worker |
| Device trust management | Implemented | Devices can be marked trusted or untrusted through the API and UI. | Add review workflows for suspicious or unknown devices. | API / UI |
| Device classification | Partially Implemented | Device types are modeled and editable, but classification is still manual. | Introduce automatic or semi-automatic classification logic. | API / UI |
| Device risk signals | Partially Implemented | Devices receive explainable risk summaries from open alerts, trust state, unknown type, first-seen time, blocked DNS and recent traffic, with detail pages showing supporting evidence. | Add known-benign review states and tune thresholds with real network data. | API / UI |
| Dashboard summary | Implemented | Dashboard endpoints and UI provide overview metrics, device activity, recent alerts, DNS domains, connection traffic/protocol widgets and device risk pills. | Add richer time-based analytics and deeper drill-down views. | API / UI |
| Alert management | Partially Implemented | Alerts are created, listed, resolved, enriched from Suricata IDS imports, and given basic lifecycle history. | Add richer alert detail, review states and false-positive handling. | API / UI |
| DNS monitoring | Partially Implemented | Stored DNS queries are exposed through a DNS overview API and Angular DNS activity page, with a configurable Pi-hole ingestion pipeline for importing DNS history. | Validate the importer against a live Pi-hole instance and add retention plus newly contacted domain detection. | API / Worker / UI |
| Network connection monitoring | Partially Implemented | Connection entities, dashboard traffic/protocol aggregation, normalized import, Zeek connection/TLS import, a paged connection overview API and an Angular connection activity page exist. | Add connection detail views and deeper traffic analytics. | API / Worker / UI |
| Pi-hole integration | Partially Implemented | A configurable Pi-hole query importer, manual API trigger, worker schedule, latest health, stale detection and import history exist, but live-appliance validation is still incomplete. | Validate response shapes against Pi-hole's local API docs. | API / Worker |
| Zeek integration | Implemented | Configurable Zeek `conn.log`, `dns.log` and `ssl.log` readers feed normalized ingestion services through manual API and worker paths with line checkpointing, import diagnostics, parser tests, health reporting, stale detection and history. | Validate against live Zeek output. | API / Worker |
| Suricata integration | Partially Implemented | A configurable Eve JSON alert importer feeds IDS alerts into GuardLAN through manual API and worker paths with checkpointing, duplicate prevention, severity mapping, evidence summaries, association, health reporting, stale detection and history. | Validate against a live Suricata sensor and add richer alert review workflows. | API / Worker |
| Integration health reporting | Implemented | Latest health state, stale state and recent import runs are recorded for Pi-hole, Zeek and Suricata and exposed through an API plus Angular Integrations page with manual import actions. | Replace schema bootstrapping with EF migrations and make thresholds configurable. | API / Worker / UI |
| SignalR real-time updates | Implemented | A protected SignalR hub, backend live-update abstraction, internal-key worker relay publisher and Angular live update service refresh dashboard, device, DNS and alert views after relevant events. | Add a backplane if the API is scaled horizontally. | API / Worker / UI |
| Authentication and authorization | Partially Implemented | Local admin login, cookie sessions, protected API controllers, protected SignalR hub and Angular route guard are implemented. | Add persistent users, roles and audit logging if GuardLAN becomes multi-user. | API / UI |
| Docker local development | Partially Implemented | A repository-root Compose setup builds and runs the UI, API, worker and PostgreSQL with health checks, auth env vars and `/api` plus `/hubs` proxying for local development. | Add EF migration tooling and production-grade secret management. | Infrastructure |
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
- The API exposes a device evidence endpoint for recent alerts, DNS queries and connections.
- The UI includes a detail page at `/devices/:id` and links to it from the dashboard and device inventory.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Api/Controllers/DevicesController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/DevicesController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/DeviceEvidenceDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/DeviceEvidenceDto.cs)
- [GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts)
- [GuardLAN.UI/src/app/features/devices/ui/device-evidence-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/device-evidence-page.component.ts)
- [GuardLAN.UI/src/app/features/devices/data-access/devices.facade.ts](../GuardLAN.UI/src/app/features/devices/data-access/devices.facade.ts)

**Missing or Incomplete**
- Device enrichment beyond hostname, type, trust state and current evidence
- Better review grouping for unknown or suspicious devices

**Next Change**
Add operator notes and review state to the device-detail experience.

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

### Device Risk Signals

**Status:** Partially Implemented

**Current State**
- Device DTOs include a risk level, score and up to four human-readable reasons.
- Risk is calculated from open alerts, device trust state, unknown device type, first-seen time, blocked DNS requests and recent connection traffic.
- The dashboard and device inventory table show risk pills and the first evidence reason.
- The devices page includes a risk summary metric and elevated-risk filter.
- The device evidence page shows the recent alerts, DNS queries and connections behind the device summary.
- Backend tests cover normal, combined-evidence and resolved-alert behavior.

**Implemented In**
- [docs/DEVICE_RISK.md](DEVICE_RISK.md)
- [GuardLAN.API/src/GuardLan.Application/Services/DeviceRiskEvaluator.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceRiskEvaluator.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/DeviceRiskDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/DeviceRiskDto.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs)
- [GuardLAN.API/tests/GuardLan.Tests/DeviceRiskEvaluatorTests.cs](../GuardLAN.API/tests/GuardLan.Tests/DeviceRiskEvaluatorTests.cs)
- [GuardLAN.UI/src/app/shared/models/network-device.ts](../GuardLAN.UI/src/app/shared/models/network-device.ts)
- [GuardLAN.UI/src/app/features/devices/data-access/devices.facade.ts](../GuardLAN.UI/src/app/features/devices/data-access/devices.facade.ts)
- [GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts)
- [GuardLAN.UI/src/app/features/devices/ui/device-evidence-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/device-evidence-page.component.ts)
- [GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts](../GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts)

**Missing or Incomplete**
- Thresholds have not been tuned against real home-network data.
- Known-benign review or suppression states do not exist yet.
- Trend signals such as newly contacted destinations are not implemented yet.

**Next Change**
Add known-benign review or suppression state, then tune thresholds against real home-network data.

### Dashboard

**Status:** Implemented

**Current State**
- The backend exposes summary and overview DTOs for the dashboard.
- The Angular dashboard displays devices, alerts, recent scans, top domains, connection traffic and protocol distribution.

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

**Status:** Partially Implemented

**Current State**
- Security alerts are stored, listed and resolved through API and UI.
- Alert creation is already wired into scan execution for discovered-device and disappearance events.
- Suricata IDS imports create alerts with source metadata, severity mapping, evidence summaries, optional connection association and lifecycle history entries.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Api/Controllers/AlertsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/AlertsController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/AlertService.cs](../GuardLAN.API/src/GuardLan.Application/Services/AlertService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/IdsAlertIngestionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/IdsAlertIngestionService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ScanExecutionService.cs)
- [GuardLAN.API/src/GuardLan.Domain/Entities/SecurityAlertHistory.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/SecurityAlertHistory.cs)
- [GuardLAN.UI/src/app/features/alerts/ui/alerts-page.component.ts](../GuardLAN.UI/src/app/features/alerts/ui/alerts-page.component.ts)
- [GuardLAN.UI/src/app/features/alerts/data-access/alerts.facade.ts](../GuardLAN.UI/src/app/features/alerts/data-access/alerts.facade.ts)

**Missing or Incomplete**
- Correlation rules beyond basic event creation
- Dedicated alert detail and history views
- False-positive, reviewed and suppressed alert states
- Notification workflows

**Next Change**
Add richer alert review states and an alert-detail view that explains related device, connection and IDS evidence.

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
- The dashboard uses connection data to calculate active-device traffic summaries, 24-hour volume and protocol distribution.
- Stored connection metadata is exposed through a dedicated connection overview API.
- Connection history supports backend pagination, protocol filtering and search.
- The backend accepts normalized connection imports with duplicate prevention and source-device matching by IP.
- The backend includes configurable Zeek `conn.log` and `ssl.log` importers with manual and scheduled ingestion paths.
- TLS observations are stored and matched to devices by source IP and connections by Zeek UID or a time-window fallback.
- The Angular UI includes a connection activity page with summary metrics, server-backed protocol filtering, search and paged recent connection history.
- The Angular dashboard includes 24-hour connection traffic and protocol distribution widgets.

**Implemented In**
- [GuardLAN.API/src/GuardLan.Domain/Entities/NetworkConnection.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/NetworkConnection.cs)
- [GuardLAN.API/src/GuardLan.Api/Controllers/ConnectionsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/ConnectionsController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ConnectionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ConnectionService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ConnectionIngestionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ConnectionIngestionService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/TlsObservationIngestionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/TlsObservationIngestionService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ZeekConnectionImportService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ZeekConnectionImportService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ZeekTlsImportService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ZeekTlsImportService.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekConnLogSource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekConnLogSource.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekSslLogSource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekSslLogSource.cs)
- [GuardLAN.API/src/GuardLan.Worker/Worker.cs](../GuardLAN.API/src/GuardLan.Worker/Worker.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/ConnectionOverviewDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/ConnectionOverviewDto.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/ConnectionIngestionDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/ConnectionIngestionDto.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/ZeekConnectionImportResultDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/ZeekConnectionImportResultDto.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/TlsObservationIngestionDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/TlsObservationIngestionDto.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/ZeekTlsImportResultDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/ZeekTlsImportResultDto.cs)
- [GuardLAN.API/src/GuardLan.Domain/Entities/TlsObservation.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/TlsObservation.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/GuardLanDbContext.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/GuardLanDbContext.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs)
- [GuardLAN.UI/src/app/features/connections/ui/connections-page.component.ts](../GuardLAN.UI/src/app/features/connections/ui/connections-page.component.ts)
- [GuardLAN.UI/src/app/features/connections/data-access/connections.facade.ts](../GuardLAN.UI/src/app/features/connections/data-access/connections.facade.ts)
- [GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts](../GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts)

**Missing or Incomplete**
- Alternative firewall log importers
- Connection detail pages and deeper traffic analytics beyond dashboard rollups

**Next Change**
Add connection detail pages and deeper traffic analytics beyond dashboard rollups.

### External Security Integrations

**Status:** Partially Implemented

**Current State**
- The repository documents integrations such as Pi-hole, Zeek and Suricata.
- Pi-hole DNS ingestion has a first backend implementation.
- Zeek DNS ingestion feeds the DNS history pipeline.
- A normalized connection ingestion contract exists for Zeek and firewall importers.
- Zeek `conn.log` and `ssl.log` ingestion now feed connection and TLS metadata through manual API and worker paths.
- Suricata Eve JSON alert ingestion feeds IDS alerts through manual API and worker paths.
- Pi-hole, Zeek and Suricata import services record their latest health state and recent import runs.
- The API exposes integration health, stale source state and import history.
- The Angular UI includes an Integrations page with manual import buttons.

**Implemented In**
- [README.md](../README.md)
- [docs/MDAC/README.md](../docs/MDAC/README.md)
- [docs/CONNECTION_INGESTION.md](CONNECTION_INGESTION.md)
- [docs/PIHOLE.md](PIHOLE.md)
- [docs/ZEEK.md](ZEEK.md)
- [docs/SURICATA.md](SURICATA.md)
- [docs/INTEGRATION_HEALTH.md](INTEGRATION_HEALTH.md)
- [GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/IntegrationHealthService.cs](../GuardLAN.API/src/GuardLan.Application/Services/IntegrationHealthService.cs)
- [GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationImportRun.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationImportRun.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ZeekConnectionImportService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ZeekConnectionImportService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ZeekDnsImportService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ZeekDnsImportService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ZeekTlsImportService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ZeekTlsImportService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/IdsAlertIngestionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/IdsAlertIngestionService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/SuricataAlertImportService.cs](../GuardLAN.API/src/GuardLan.Application/Services/SuricataAlertImportService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/ConnectionIngestionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/ConnectionIngestionService.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekConnLogSource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekConnLogSource.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekDnsLogSource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekDnsLogSource.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekSslLogSource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Zeek/ZeekSslLogSource.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Suricata/SuricataEveJsonSource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Suricata/SuricataEveJsonSource.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Dns/PiHoleDnsQuerySource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Dns/PiHoleDnsQuerySource.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationHealthRepository.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationHealthRepository.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationImportRunRepository.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationImportRunRepository.cs)
- [GuardLAN.UI/src/app/features/integrations/ui/integrations-page.component.ts](../GuardLAN.UI/src/app/features/integrations/ui/integrations-page.component.ts)

**Missing or Incomplete**
- Live validation for Pi-hole integration
- Live validation against Zeek output from a running sensor
- Live validation against Suricata output from a running sensor
- EF migrations for integration health and import run tables
- Configurable freshness thresholds per source

**Next Change**
Replace ad hoc schema bootstrapping with EF migrations.

### Suricata Integration

**Status:** Partially Implemented

**Current State**
- The backend can read Suricata `eve.json` alert rows from a configured file.
- The importer skips non-alert Eve rows and malformed records.
- Imported IDS alerts are normalized into GuardLAN security alerts with source metadata, severity mapping and evidence summaries.
- Device matching uses source IP first and destination IP second.
- Connection matching uses known endpoints, destination ports and a five-minute time window.
- The API exposes a manual Suricata import endpoint and the worker can run scheduled imports with line-number checkpointing.

**Implemented In**
- [docs/SURICATA.md](SURICATA.md)
- [GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/IdsAlertIngestionService.cs](../GuardLAN.API/src/GuardLan.Application/Services/IdsAlertIngestionService.cs)
- [GuardLAN.API/src/GuardLan.Application/Services/SuricataAlertImportService.cs](../GuardLAN.API/src/GuardLan.Application/Services/SuricataAlertImportService.cs)
- [GuardLAN.API/src/GuardLan.Application/Suricata/ISuricataAlertSource.cs](../GuardLAN.API/src/GuardLan.Application/Suricata/ISuricataAlertSource.cs)
- [GuardLAN.API/src/GuardLan.Infrastructure/Suricata/SuricataEveJsonSource.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Suricata/SuricataEveJsonSource.cs)
- [GuardLAN.API/src/GuardLan.Worker/Worker.cs](../GuardLAN.API/src/GuardLan.Worker/Worker.cs)
- [GuardLAN.API/tests/GuardLan.Tests/ZeekIngestionTests.cs](../GuardLAN.API/tests/GuardLan.Tests/ZeekIngestionTests.cs)

**Missing or Incomplete**
- Live validation against a real Suricata sensor
- False-positive and reviewed alert states
- Dedicated IDS alert detail and history UI
- EF migrations for integration health and import run tables

**Next Change**
Validate against live Suricata Eve output and add alert review states.

### SignalR Real-Time Updates

**Status:** Implemented

**Current State**
- The API exposes a SignalR hub at `/hubs/guardlan`.
- Application services publish through an `ILiveUpdatePublisher` abstraction.
- API-hosted services broadcast directly through `IHubContext`.
- The worker process publishes scan and ingestion events by calling an internal API relay endpoint protected by `X-GuardLAN-Internal-Key`.
- Live events cover scan queueing, scan completion, scan failure, new devices, device online/offline changes, new alerts, alert resolution and DNS ingestion completion.
- The Angular app starts one authenticated live update connection and refreshes dashboard, devices, DNS and alert pages when relevant events arrive.

**Implemented In**
- [README.md](../README.md)
- [docs/LIVE_UPDATES.md](LIVE_UPDATES.md)
- [GuardLAN.UI/docs/FRONTEND_ARCHITECTURE.md](../GuardLAN.UI/docs/FRONTEND_ARCHITECTURE.md)
- [GuardLAN.API/src/GuardLan.Api/Hubs/GuardLanHub.cs](../GuardLAN.API/src/GuardLan.Api/Hubs/GuardLanHub.cs)
- [GuardLAN.API/src/GuardLan.Api/Realtime/SignalRLiveUpdatePublisher.cs](../GuardLAN.API/src/GuardLan.Api/Realtime/SignalRLiveUpdatePublisher.cs)
- [GuardLAN.API/src/GuardLan.Application/Abstractions/ILiveUpdatePublisher.cs](../GuardLAN.API/src/GuardLan.Application/Abstractions/ILiveUpdatePublisher.cs)
- [GuardLAN.API/src/GuardLan.Application/Models/LiveUpdateDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/LiveUpdateDto.cs)
- [GuardLAN.API/src/GuardLan.Api/Controllers/InternalLiveUpdatesController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/InternalLiveUpdatesController.cs)
- [GuardLAN.API/src/GuardLan.Worker/Realtime/ApiWorkerLiveUpdatePublisher.cs](../GuardLAN.API/src/GuardLan.Worker/Realtime/ApiWorkerLiveUpdatePublisher.cs)
- [GuardLAN.UI/src/app/shared/live-updates/live-updates.service.ts](../GuardLAN.UI/src/app/shared/live-updates/live-updates.service.ts)

**Missing or Incomplete**
- No SignalR backplane is configured for multiple API instances.
- No persistent notification feed exists in the UI.

**Next Change**
Add a SignalR backplane only if the API is scaled horizontally.

### Authentication and Authorization

**Status:** Partially Implemented

**Current State**
- The API uses cookie authentication with one configured local admin account.
- `GET /api/auth/session`, `POST /api/auth/login` and `POST /api/auth/logout` are implemented.
- A fallback authorization policy protects regular API controllers by default.
- Health endpoints remain anonymous for Docker and local service checks.
- The SignalR hub requires an authenticated session.
- The Angular app includes a login page, auth guard, auth service and credentialed API interceptor.
- The worker live-update relay uses a shared internal publisher key instead of a browser-accessible publish method.

**Implemented In**
- [docs/SECURITY_HARDENING.md](SECURITY_HARDENING.md)
- [GuardLAN.API/src/GuardLan.Api/Auth/GuardLanAuthOptions.cs](../GuardLAN.API/src/GuardLan.Api/Auth/GuardLanAuthOptions.cs)
- [GuardLAN.API/src/GuardLan.Api/Auth/LocalUserAuthenticator.cs](../GuardLAN.API/src/GuardLan.Api/Auth/LocalUserAuthenticator.cs)
- [GuardLAN.API/src/GuardLan.Api/Auth/InternalPublisherKeyValidator.cs](../GuardLAN.API/src/GuardLan.Api/Auth/InternalPublisherKeyValidator.cs)
- [GuardLAN.API/src/GuardLan.Api/Controllers/AuthController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/AuthController.cs)
- [GuardLAN.API/src/GuardLan.Api/Controllers/InternalLiveUpdatesController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/InternalLiveUpdatesController.cs)
- [GuardLAN.UI/src/app/features/auth/login-page.component.ts](../GuardLAN.UI/src/app/features/auth/login-page.component.ts)
- [GuardLAN.UI/src/app/shared/auth/auth.service.ts](../GuardLAN.UI/src/app/shared/auth/auth.service.ts)
- [GuardLAN.UI/src/app/shared/auth/auth.guard.ts](../GuardLAN.UI/src/app/shared/auth/auth.guard.ts)

**Missing or Incomplete**
- Persistent multi-user identity
- Role-based or permission-based access control
- Audit logging for login and administrative actions

**Next Change**
Add persistent users or external identity only if GuardLAN needs more than one local operator.

### Docker and Local Deployment

**Status:** Partially Implemented

**Current State**
- A repository-root Compose setup builds and runs the UI, API, worker and PostgreSQL.
- Service health checks and the UI Nginx `/api` plus `/hubs` proxy are configured for local development.
- Compose passes local auth credentials and the worker internal publisher key through environment variables.
- A PostgreSQL-only compose file still exists under [GuardLAN.API/docker-compose.yml](../GuardLAN.API/docker-compose.yml) for backend-focused workflows.

**Implemented In**
- [compose.yml](../compose.yml)
- [GuardLAN.API/Dockerfile](../GuardLAN.API/Dockerfile)
- [GuardLAN.API/Dockerfile.worker](../GuardLAN.API/Dockerfile.worker)
- [GuardLAN.UI/Dockerfile](../GuardLAN.UI/Dockerfile)
- [GuardLAN.UI/nginx.conf](../GuardLAN.UI/nginx.conf)
- [docs/Docker.md](Docker.md)
- [GuardLAN.API/docker-compose.yml](../GuardLAN.API/docker-compose.yml)
- [GuardLAN.API/src/GuardLan.Api/Program.cs](../GuardLAN.API/src/GuardLan.Api/Program.cs)
- [docs/SECURITY_HARDENING.md](SECURITY_HARDENING.md)

**Missing or Incomplete**
- EF migration workflow for containerized development
- Production-grade secret store integration

**Next Change**
Add EF migration tooling and replace development `EnsureCreated`.

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

EF migrations and database schema management.

Phase 10 now records stale source state and import history for Pi-hole, Zeek and Suricata, and exposes manual import actions from the Integrations page. The next implementation slice should replace the current schema bootstrapping with EF migrations.

## Known Technical Gaps

- Authentication is single-admin only; persistent users, roles and audit logging are future work.
- Live updates have no cross-instance SignalR backplane yet.
- External integrations are only partially normalized behind shared ingestion contracts.
- Device risk does not yet include suppression states or tuned behavioral baselines.
- Integration freshness thresholds are fixed at 15 minutes and should become configurable per source type.
- Integration health and history tables use narrow schema bootstrapping until EF migrations are added.
- DNS visibility has an API, UI and Pi-hole importer, but the importer still needs live validation.
- Suricata IDS alert ingestion still needs live sensor validation and richer review workflows.
- The local deployment story is containerized for first-run development, but production hardening remains incomplete.

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
