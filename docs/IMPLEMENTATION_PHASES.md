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

Status: active.

Started:

* Stored DNS query overview API
* DNS history page in the Angular app
* Top domains and top clients summaries from stored DNS data

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
3. Add Pi-hole configuration and validate connectivity.
4. Implement ingestion with idempotency and client-IP device matching.
5. Add new-domain alerts and retention cleanup.

## Phase 2: Network Connection Model

Goal: represent connection metadata without packet-content capture.

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

## Phase 3: Zeek Ingestion

Goal: populate DNS, connection, TLS, and protocol metadata from structured Zeek logs.

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

Deliverables:

* Suricata Eve JSON importer
* IDS alert normalization
* Device and connection association
* Severity mapping
* Evidence payload summaries
* Alert lifecycle history
* False-positive review workflow

## Phase 5: SignalR Live Updates

Goal: make operational changes visible without manual refresh.

Deliverables:

* New device notifications
* Scan completion notifications
* New alert notifications
* Online and offline status updates
* DNS ingestion status updates
* Angular live update service

## Phase 6: Authentication and Deployment Hardening

Goal: protect the dashboard once it contains genuinely sensitive telemetry.

Deliverables:

* Local-user authentication
* Session management
* HTTPS-first deployment configuration
* API authorization policies
* Secret handling
* Backup and restore guidance
* Deployment documentation

## Deferred: Device Risk Classification

Heavy automated risk classification should wait until DNS, connection, and IDS ingestion exist.

Without those signals, GuardLAN does not have enough evidence to classify device risk reliably. Early labels should stay explainable and evidence-based.
