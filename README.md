# GuardLAN

GuardLAN is an experimental network visibility and security-monitoring platform for personal home environments.

The project explores what can realistically be observed inside a local network, how that information can be normalized and analyzed, and how it can be presented through a clear SOC-inspired dashboard.

The goal is to build a central overview of connected devices, DNS activity, network connections, protocols, traffic metadata, and security alerts without requiring a full enterprise security stack.

> GuardLAN is intended for research, learning, and authorized monitoring of personally owned networks and devices.

## Project Status

GuardLAN is under active development and should be considered a research and engineering project.

The current implementation includes:

* A separated backend and frontend workspace: `GuardLAN.API` and `GuardLAN.UI`
* ASP.NET Core Web API with application, domain, infrastructure, and worker projects
* PostgreSQL persistence through Entity Framework Core
* Device, alert, dashboard, and scan API endpoints
* A queued network scanner flow backed by a worker service
* An Angular dashboard wired to the backend overview endpoint
* DNS overview API and Angular DNS activity page for stored DNS query data
* Connection overview API, normalized connection import endpoint, Zeek connection/DNS/TLS importers, dashboard traffic widgets and Angular connection activity page
* Suricata Eve JSON alert importer with device and connection association
* SignalR live updates for scan, device, alert and DNS ingestion changes
* Local-user authentication with cookie sessions for the API, UI and SignalR hub
* Explainable device risk signals based on alerts, DNS, connection and inventory evidence
* Device evidence drill-down pages for inspecting recent alerts, DNS queries and connections
* Local Docker Compose infrastructure for the UI, API, worker and PostgreSQL

GuardLAN is not intended to replace a commercial SIEM, IDS, endpoint detection platform, or managed SOC service.

## First Start

To run GuardLAN locally for the first time, use the repository-root Docker setup:

```bash
docker compose up --build
```

This starts:

- the Angular UI at http://localhost:4200
- the ASP.NET Core API at http://localhost:5232
- the scanner and ingestion worker
- PostgreSQL at localhost:5432

The development login is:

```text
Username: guardlan
Password: guardlan
```

Change `GuardLanAuth__AdminPassword` and `GuardLanAuth__InternalPublisherKey` in `.env` before using GuardLAN beyond a local development machine.

Useful follow-up commands:

```bash
docker compose down
docker compose logs -f api
docker compose logs -f worker
docker compose ps
```

If you want to reset the local database volume completely, run:

```bash
docker compose down -v
```

For more details, see [docs/Docker.md](docs/Docker.md).

## Objectives

GuardLAN aims to answer questions such as:

* Which devices are currently connected to the network?
* When was a device first and last observed?
* Which devices are new, unknown, or untrusted?
* Which external domains are devices contacting?
* Which protocols and destination ports are being used?
* Which devices generate the most network activity?
* Are there suspicious or unusual communication patterns?
* Can information from multiple monitoring tools be presented through one interface?
* Which network activity remains visible despite modern encryption?
* How useful can a lightweight home SOC become?

## Planned Capabilities

### Device Discovery

* Detect devices connected to the local network
* Track IP and MAC addresses
* Resolve hostnames and device vendors
* Identify new and unknown devices
* Track first-seen and last-seen timestamps
* Display online and offline status
* Classify devices by type
* Mark known devices as trusted

### DNS Monitoring

* Display DNS requests by device
* Show allowed and blocked requests
* Identify frequently contacted domains
* Detect newly observed domains
* Integrate with Pi-hole or similar DNS services
* Analyze unusual DNS behavior

### Network Activity

* Display source and destination information
* Track protocols and destination ports
* Show connection duration
* Display transferred data volume
* Group connections by device and destination
* Ingest structured network metadata from tools such as Zeek

### Security Monitoring

* Display security alerts
* Integrate Suricata IDS events
* Classify alerts by severity and status
* Associate alerts with known devices
* Track unresolved and resolved findings
* Highlight unusual device behavior
* Support future notification integrations

### Dashboard

* Connected device overview
* Unknown device warnings
* Active security alerts
* DNS request statistics
* Most active devices
* Most contacted destinations
* Network activity over time
* Protocol distribution
* Recent events
* Responsive Angular interface

## Visibility and Encryption

GuardLAN focuses primarily on network metadata rather than decrypting private traffic.

Depending on the available data source, the platform may be able to observe:

* Connected devices
* IP addresses
* MAC addresses
* Hostnames
* DNS requests
* Destination addresses
* Protocols
* Ports
* Connection duration
* Traffic volume
* TLS metadata
* IDS alerts

For encrypted HTTPS traffic, GuardLAN will normally not be able to display the exact content of requests, searches, messages, or transmitted data.

For example, the system may identify that a device contacted a specific domain over HTTPS, but it will generally not know the exact page, search query, or transmitted content.

This limitation is intentional. The project focuses on useful network visibility without requiring invasive TLS interception.

## Architecture

GuardLAN follows a separated frontend and backend architecture.

```text
Network Data Sources
    |
    |-- Local network scanner
    |-- Router or firewall
    |-- Pi-hole
    |-- Zeek
    |-- Suricata
    |-- Future integrations
    |
ASP.NET Core Backend
    |
PostgreSQL
    |
Angular Frontend
```

The repository is split into two top-level application folders:

```text
GuardLAN.API/
  src/
    GuardLan.Api/
    GuardLan.Application/
    GuardLan.Domain/
    GuardLan.Infrastructure/
    GuardLan.Worker/
  docs/

GuardLAN.UI/
  src/
  docs/
```

## Backend

The backend is responsible for:

* Data collection
* Data normalization
* Business logic
* Device classification
* Security evaluation
* Data aggregation
* Filtering
* Sorting
* Pagination
* Integration with external monitoring tools
* Providing purpose-built API response DTOs

The backend follows:

* ASP.NET Core Web API
* Entity Framework Core
* Unit of Work pattern
* Generic repositories
* Entity-specific repositories
* Service layer
* Request and response DTOs
* Dependency injection
* Asynchronous processing
* Background workers

The API does not expose persistence entities directly.

## Frontend

The frontend is primarily responsible for presentation and user interaction.

It follows:

* Angular standalone components
* Feature-based architecture
* Typed API clients
* Signals for synchronous UI state
* RxJS for asynchronous streams
* Reusable UI patterns
* Responsive layouts
* Accessible interface components

The frontend intentionally contains very little business logic.

Whenever practical, one page load or explicit user action should result in one HTTP request. The backend provides purpose-built DTOs containing the complete data required for the corresponding view.

## Technology Stack

### Frontend

* Angular 20
* TypeScript
* Signals
* RxJS
* SCSS

### Backend

* ASP.NET Core 10
* C#
* Entity Framework Core
* PostgreSQL
* Background services
* REST API
* SignalR live updates

### Infrastructure

* Docker
* Docker Compose
* Nmap
* Optional Linux deployment
* Optional Proxmox deployment
* Optional managed switch with port mirroring

### Planned Integrations

* Nmap
* Pi-hole
* OPNsense
* Zeek
* Suricata
* Wazuh
* Security Onion

Not every listed integration is currently implemented.

## Initial Development Scope

The first version focuses on device visibility.

Planned functionality includes:

* Scan a configured local subnet
* Detect connected devices
* Store device information
* Track first-seen and last-seen timestamps
* Mark devices online or offline
* Allow users to assign display names
* Allow users to classify devices
* Mark devices as trusted or unknown
* Generate alerts for newly discovered devices
* Display all information through an Angular dashboard

The initial version will not attempt to inspect full packet contents.

## Run Locally

Prerequisites:

* .NET 10 SDK
* Node.js and npm
* Docker Desktop or another Docker engine
* Nmap on `PATH` for real network discovery

Start PostgreSQL:

```powershell
cd GuardLAN.API
docker compose up -d postgres
```

Run the API:

```powershell
dotnet run --project src/GuardLan.Api/GuardLan.Api.csproj --launch-profile http
```

Run the scanner worker in a second terminal:

```powershell
cd GuardLAN.API
dotnet run --project src/GuardLan.Worker/GuardLan.Worker.csproj
```

Run the Angular app in a third terminal:

```powershell
cd GuardLAN.UI
npm install
npm start
```

The Angular dev server proxies `/api` and `/hubs` requests to `http://localhost:5232`.

Useful local URLs:

```text
UI:  http://localhost:4200
API: http://localhost:5232
```

Useful API endpoints:

```text
GET   http://localhost:5232/api/health
GET   http://localhost:5232/api/auth/session
POST  http://localhost:5232/api/auth/login
POST  http://localhost:5232/api/auth/logout
GET   http://localhost:5232/api/dashboard
GET   http://localhost:5232/api/dashboard/overview
GET   http://localhost:5232/api/connections/overview?page=1&pageSize=25&protocol=tcp&search=443
POST  http://localhost:5232/api/connections/import
POST  http://localhost:5232/api/connections/import/zeek
POST  http://localhost:5232/api/integrations/zeek/import
POST  http://localhost:5232/api/integrations/suricata/import
GET   http://localhost:5232/api/dns/overview
POST  http://localhost:5232/api/dns/import/pihole
GET   http://localhost:5232/api/devices
PATCH http://localhost:5232/api/devices/{id}
GET   http://localhost:5232/api/alerts
PATCH http://localhost:5232/api/alerts/{id}/resolve
GET   http://localhost:5232/api/scans
GET   http://localhost:5232/api/scans/{id}
POST  http://localhost:5232/api/scans
```

The worker processes queued scans using `nmap -sn <subnet>`, so nmap must be installed and available on `PATH` before real network discovery will run.

## Validation

Build the backend:

```powershell
dotnet build GuardLAN.API/GuardLAN.API.sln
```

Build the frontend:

```powershell
cd GuardLAN.UI
npm run build
```

Run the frontend tests:

```powershell
cd GuardLAN.UI
npm test -- --watch=false --browsers=ChromeHeadless
```

## Research Questions

GuardLAN is intended to document practical findings while the system is being developed.

Areas of investigation include:

* Which discovery techniques work reliably across home networks?
* How accurately can devices be identified using network metadata?
* How much visibility is lost through encrypted DNS and HTTPS?
* Which information can be collected without installing endpoint agents?
* How much traffic metadata is useful before it becomes noise?
* Which indicators are useful for detecting unusual home-network behavior?
* How should events from multiple tools be normalized?
* What information belongs on a useful security dashboard?
* Which enterprise SOC concepts can reasonably be adapted for home use?
* How can false positives be reduced in a small network?

## Security and Privacy

GuardLAN must only be used on networks and devices that the operator owns or is explicitly authorized to monitor.

Network metadata can reveal sensitive information, including:

* Devices in use
* Online activity patterns
* Contacted services
* Usage times
* Internal addressing
* Device names
* Security weaknesses

Collected data must therefore be treated as sensitive.

Recommended deployment practices include:

* Keep the application inside the local network
* Do not expose the dashboard directly to the public internet
* Use authentication
* Use HTTPS
* Restrict database access
* Protect API credentials
* Avoid storing unnecessary packet payloads
* Define data-retention limits
* Back up configuration separately from collected telemetry
* Segment monitoring infrastructure from untrusted devices

## Legal Notice

This software is intended solely for authorized security research, education, and monitoring.

Users are responsible for ensuring that their use of the software complies with applicable privacy, telecommunications, and computer-security laws.

Do not use GuardLAN to monitor networks, systems, or people without appropriate authorization.

## Documentation

Detailed project conventions are documented separately:

* [Implementation phases](docs/IMPLEMENTATION_PHASES.md)
* [Docker local setup](docs/Docker.md)
* [Connection ingestion](docs/CONNECTION_INGESTION.md)
* [Pi-hole integration](docs/PIHOLE.md)
* [Zeek integration](docs/ZEEK.md)
* [Suricata integration](docs/SURICATA.md)
* [SignalR live updates](docs/LIVE_UPDATES.md)
* [Security hardening](docs/SECURITY_HARDENING.md)
* [Device risk signals](docs/DEVICE_RISK.md)
* [Device evidence drill-down](docs/DEVICE_EVIDENCE.md)
* [MDAC mobile collector plan](docs/MDAC/README.md)
* [Backend architecture](GuardLAN.API/docs/BACKEND_ARCHITECTURE.md)
* [Frontend architecture](GuardLAN.UI/docs/FRONTEND_ARCHITECTURE.md)
* [Backend agent instructions](GuardLAN.API/AGENTS.md)
* [Frontend agent instructions](GuardLAN.UI/AGENTS.md)

Coding agents and contributors should review the relevant architecture documentation before introducing new endpoints, pages, services, components, or integrations.

## Contributing

GuardLAN is currently a personal research project, but structured contributions may be accepted in the future.

Contributions should:

* Follow the existing architecture
* Preserve clear separation of responsibilities
* Use typed DTOs
* Avoid unnecessary frontend business logic
* Avoid unnecessary HTTP requests
* Include tests for important behavior
* Document new external integrations
* Respect security and privacy requirements

Major architectural changes should be discussed before implementation.

## Current Limitations

Expected early limitations include:

* Device discovery accuracy depends on the network environment
* Some devices may not respond to active scans
* MAC randomization can affect device identification
* Encrypted DNS may bypass local DNS monitoring
* HTTPS payload contents remain encrypted
* Vendor identification may be incomplete
* Network monitoring may require router, firewall, or switch configuration
* Wireless traffic visibility depends on the access-point architecture
* IDS rules may generate false positives
* Detection results should not be treated as definitive proof of compromise

## Disclaimer

GuardLAN is experimental software.

It is not guaranteed to detect attacks, malware, unauthorized access, or data loss. It must not be treated as the only security control protecting a network.

The project exists to explore network visibility, cybersecurity monitoring, and security-dashboard engineering in a practical home-lab environment.

## License

A license has not yet been selected.
