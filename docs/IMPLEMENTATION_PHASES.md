# GuardLAN Implementation Phases

This roadmap is based on the current repository state and the remaining planned capabilities.

## Phase 0: Device Visibility Foundation

Status: complete enough for DNS work.

Implemented:

* ASP.NET Core backend, Angular frontend, PostgreSQL persistence, and worker projects
* Device inventory model and editable UI
* Queued network scan workflow
* Scanner worker integration point
* Dashboard overview endpoint and dashboard UI
* Basic security alerts and alert review workflow
* MDAC mobile collector planning documentation

Continue improving this phase only when needed by later ingestion work.

## Phase 1: DNS Visibility

Goal: make DNS activity visible quickly, using Pi-hole as the first practical data source.

Status: partially implemented.

Started:

* Stored DNS query overview API
* DNS history page in the Angular app
* Top domains and top clients summaries from stored DNS data
* Configurable Pi-hole DNS query importer
* Manual and scheduled DNS ingestion entry points
* Duplicate prevention and client-IP device matching during import

Deliverables:

* DNS overview and history API
* DNS history page in the Angular app
* Pi-hole connection configuration
* Pi-hole API client or log importer
* Periodic ingestion worker
* Duplicate prevention
* Device matching by client IP
* Allowed and blocked query tracking
* DNS summary on device details
* Top domains dashboard widget
* Unknown or newly contacted domain detection
* Retention and cleanup configuration

Work order:

1. Expose stored DNS data through purpose-built backend DTOs.
2. Add a DNS page that uses one initial overview request.
3. Add Pi-hole configuration and a manual import path.
4. Implement ingestion with idempotency and client-IP device matching.
5. Validate against a live Pi-hole instance and record import diagnostics.
6. Add new-domain alerts and retention cleanup.

## Phase 2: Network Connection Model

Goal: represent connection metadata without packet-content capture.

Status: complete enough for Zeek ingestion work.

Started:

* Stored connection overview API
* Connection activity page in the Angular app
* Protocol, destination and device traffic summaries from stored connection data
* Backend pagination and server-side protocol/search filtering for connection history
* Normalized connection import endpoint with duplicate prevention and device matching
* Dashboard protocol and traffic summary widgets

Deliverables:

* Source device
* Destination IP and optional domain
* Protocol
* Destination port
* Bytes sent and received
* Start and end timestamps
* Connection history API
* Connection view in the Angular app
* Dashboard protocol and traffic summaries

Work order:

1. Expose stored connection data through purpose-built backend DTOs.
2. Add a connection page that uses one initial overview request.
3. Add backend pagination and server-side filtering for growing history.
4. Define the normalized ingestion contract for Zeek connection records.
5. Add dashboard protocol and traffic summary widgets.

## Phase 3: Zeek Ingestion

Goal: populate DNS, connection, TLS, and protocol metadata from structured Zeek logs.

Status: complete enough for Suricata ingestion work.

Started:

* Configurable Zeek `conn.log` reader
* Manual Zeek connection import endpoint
* Configurable Zeek `dns.log` reader
* Configurable Zeek `ssl.log` reader for TLS metadata
* Aggregate manual Zeek import endpoint
* Scheduled worker path for Zeek imports
* Line-number checkpointing for incremental Zeek log reads
* Mapping from Zeek connection rows into the normalized connection ingestion contract
* Mapping from Zeek DNS rows into DNS history
* TLS observation storage with device and connection matching
* Tests for malformed rows, delayed/appended rows and duplicate connection records

Deliverables:

* Zeek log import worker
* Normalization services for `conn.log`, `dns.log`, and TLS metadata
* Duplicate prevention and checkpointing
* Device matching by IP and time window
* Import diagnostics
* Tests for malformed, delayed, and duplicate records

GuardLAN should ingest structured Zeek output rather than capturing and parsing packets directly inside the application.

## Phase 4: Suricata Integration

Goal: import real IDS alerts and connect them to GuardLAN devices and network evidence.

Status: complete enough for SignalR live update work.

Started:

* Configurable Suricata Eve JSON alert reader
* Manual Suricata import endpoint
* Scheduled worker path for Suricata imports
* Line-number checkpointing for incremental `eve.json` reads
* IDS alert normalization and duplicate prevention
* Device matching by source or destination IP
* Connection matching by endpoints and time window
* Severity mapping, evidence summaries and alert lifecycle history
* Parser tests for alert, non-alert and invalid Eve rows

Deliverables:

* Suricata Eve JSON importer
* IDS alert normalization
* Device and connection association
* Severity mapping
* Evidence payload summaries
* Alert lifecycle history
* False-positive review workflow

Remaining hardening:

* Validate against a live Suricata sensor
* Add duplicate suppression rules if real sensors produce noisy repeated signatures

## Phase 5: SignalR Live Updates

Goal: make operational changes visible without manual refresh.

Status: complete enough for authentication and deployment hardening work.

Started:

* SignalR hub at `/hubs/guardlan`
* Backend live update abstraction
* API publisher backed by `IHubContext`
* Worker publisher that relays events through an internal API endpoint
* Live events for scan queueing, scan completion, scan failure, new devices, device online/offline changes, new alerts, alert lifecycle changes and DNS ingestion completion
* Angular live update service with reconnect behavior
* Dashboard, device, DNS and alert page refresh hooks
* Docker Compose worker service and `/hubs` proxy support

Deliverables:

* New device notifications
* Scan completion notifications
* New alert notifications
* Online and offline status updates
* DNS ingestion status updates
* Angular live update service

Remaining hardening:

* Add a cross-instance backplane if the API is scaled horizontally
* Add user-facing notification history or toast UI if needed

## Phase 6: Authentication and Deployment Hardening

Goal: protect the dashboard once it contains genuinely sensitive telemetry.

Status: complete enough for evidence-based device risk work.

Started:

* Local admin login endpoint
* Cookie-based session management
* Angular login page, auth guard and session service
* Global API authorization fallback policy
* Authenticated SignalR hub
* Internal-key worker live-update relay
* Docker auth and internal relay configuration
* Secret handling, HTTPS, backup and restore documentation

Deliverables:

* Local-user authentication
* Session management
* HTTPS-first deployment configuration
* API authorization policies
* Secret handling
* Backup and restore guidance
* Deployment documentation

Remaining hardening:

* Replace single-admin configuration with persistent users or an external identity provider if GuardLAN becomes multi-user
* Add role-based authorization once distinct operator roles exist
* Add structured audit logging for login and administrative actions

## Phase 7: Evidence-Based Device Risk Signals

Goal: prioritize device review using explainable evidence that GuardLAN already collects.

Status: complete.

Started:

* Device risk DTO with level, score and human-readable reasons
* `IDeviceRiskEvaluator` application service
* Risk scoring from open IDS alerts, trust state, unknown device type, first-seen time, blocked DNS queries and recent traffic volume
* Device inventory API responses include risk summaries
* Dashboard overview device rows include risk summaries
* Angular dashboard and devices pages show risk pills and first reason
* Devices page includes a risk summary count and risk filter
* Unit tests cover normal, combined-evidence and resolved-alert behavior

Deliverables:

* Explainable device risk summary
* Evidence-based scoring from DNS, connection, IDS and inventory data
* Device list and dashboard risk display
* Elevated-risk filter
* Tests for scoring behavior

Remaining hardening:

* Add device-level known-benign policy if alert-level suppression is not enough
* Tune thresholds against real home-network data
* Add behavior trends such as newly contacted destinations or unusual traffic volume

## Phase 8: Device Evidence Drill-Down

Goal: let operators inspect the telemetry behind a device risk summary.

Status: complete enough for integration health reporting work.

Started:

* Device evidence DTO with device summary, risk, evidence counters and recent telemetry lists
* `GET /api/devices/{id}/evidence` endpoint
* Targeted repository queries for recent per-device alerts, DNS queries and connections
* Angular route at `/devices/:id`
* Device evidence page with risk reasons, summary metrics, recent alerts, recent DNS and recent connections
* Dashboard and device inventory links into the detail route

Deliverables:

* Device evidence API
* Single-request Angular detail page
* Recent alert, DNS and connection evidence lists
* Links from device inventory and dashboard

Remaining hardening:

* Add deep links into future DNS and connection detail pages
* Add device-level operator notes or known-benign policy
* Add longer-range trend comparison after historical baselines exist

## Phase 9: Integration Health Reporting

Goal: show whether telemetry sources are enabled, reachable and recently checked.

Status: complete.

Started:

* Persistent `IntegrationHealth` domain model
* Health status values for healthy, warning, unavailable and disabled sources
* Application health recorder and overview DTO
* Health recording in Pi-hole DNS, Zeek connection, Zeek DNS, Zeek TLS and Suricata import paths
* `GET /api/integrations/health` endpoint
* Angular Integrations page with summary cards and source table
* Sidebar navigation to the Integrations page
* Documentation for health status rules and current storage approach

Deliverables:

* Latest health state per integration source
* Summary endpoint for UI and operators
* UI source health table
* Manual and worker import health recording

Moved forward:

* Database bootstrap was replaced by EF migrations in Phase 11.

## Phase 10: Integration Freshness and Import History

Goal: show stale sources, preserve recent import runs and let operators trigger imports manually.

Status: complete.

Started:

* `Stale` integration health status
* Configurable stale threshold for previously healthy or warning sources, with a 15-minute default
* Persistent `IntegrationImportRun` history model
* Recent import runs returned by `GET /api/integrations/health`
* Angular summary card for stale sources
* Recent import run table on the Integrations page
* Manual import buttons for Pi-hole, Zeek and Suricata

Deliverables:

* Stale source detection
* Import run history
* Manual import actions from the UI
* Updated integration health docs

Remaining hardening:

* Add history filters if the run list grows

## Phase 11: EF Migrations and Schema Management

Goal: make PostgreSQL schema changes explicit, repeatable and reviewable.

Status: complete.

Started:

* EF Core design-time package on the API startup project
* Initial EF migration for the current PostgreSQL schema
* GuardLAN database model snapshot
* Development startup migration application before seeding
* Removal of integration health table bootstrap code
* Docker and integration health documentation for migration workflow

Deliverables:

* `InitialCreate` migration covering devices, DNS queries, connections, TLS observations, alerts, scan runs, integration health and integration import history
* Single EF-managed schema source for local development
* Commands for listing, adding and applying migrations

Remaining hardening:

* Decide the production migration runner/deployment policy before exposing GuardLAN beyond local development
* Add backup and retention automation

## Phase 12: Alert Review Lifecycle

Goal: let operators triage alerts without deleting evidence or losing history.

Status: complete.

Started:

* `AlertReviewStatus` values for open, reviewed, resolved, false-positive and suppressed alerts
* Review timestamp and latest review note on each alert
* Lifecycle history entries for review, resolve, false-positive, suppress and reopen actions
* API endpoints for each lifecycle action
* `alertUpdated` live update event for non-resolve lifecycle changes
* Alerts page filters, summary metrics, note field and context-aware lifecycle buttons
* Migration updates for MDAC tables already present in the model and alert review columns

Deliverables:

* Operator-visible false-positive handling
* Suppression and reopen flow
* Reviewed-but-still-open alert state
* Dashboard and device evidence refresh after alert lifecycle changes

Remaining hardening:

* Add duplicate/noisy signature suppression rules after live IDS validation

## Phase 13: Alert Detail and History Drilldown

Goal: let operators inspect a single alert with its full lifecycle and related evidence.

Status: complete.

Started:

* `GET /api/alerts/{id}` detail endpoint
* Alert detail DTO with the alert, lifecycle history and related connection metadata
* Repository loading for alert device, connection device and history data
* Angular route at `/alerts/:id`
* Alert detail page with source metadata, related device, related flow, review note and lifecycle actions
* Links from the alert queue and device evidence recent-alert list

Deliverables:

* Single-alert API read model
* Full lifecycle history view
* Related device and connection evidence
* Review, resolve, false-positive, suppress and reopen controls from the detail page

Remaining hardening:

* Add duplicate/noisy signature suppression rules after live IDS validation
* Add notification workflows once alert volume and operator needs are clearer
