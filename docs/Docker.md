You are working inside the GuardLAN repository.

The repository is intentionally split into two top-level workspaces:

* `GuardLAN.API` contains the ASP.NET Core backend, background workers, persistence, database migrations and backend architecture documentation.
* `GuardLAN.UI` contains the Angular frontend and frontend architecture documentation.

Your task is to create a complete local Docker development and first-run setup for GuardLAN.

The final result must allow a developer to clone the repository and start the complete application with one command:

```bash
docker compose up --build
```

The complete local environment must contain:

1. GuardLAN Angular UI
2. GuardLAN ASP.NET Core API
3. PostgreSQL database

The setup must include Dockerfiles, Docker Compose configuration, environment configuration, database persistence, health checks and complete documentation for running the project for the first time.

Do not only create an example or partial configuration. Inspect the existing repository, project files, ports, startup projects, Angular configuration, backend configuration and existing documentation before implementing the solution.

# Main Goal

After cloning the repository, a new developer should be able to:

1. Install the required tools.
2. Configure the required environment variables.
3. Build all Docker images.
4. Start the UI, API and database.
5. Apply or verify database migrations.
6. Open the Angular UI.
7. Open the API Swagger page.
8. Verify that the API can connect to PostgreSQL.
9. Stop and restart the environment without losing database data.
10. Reset the complete local environment when required.

The setup must be understandable without requiring undocumented manual steps.

# Required Repository-Level Files

At the repository root, create or update:

```text
GuardLAN/
├── GuardLAN.API/
├── GuardLAN.UI/
├── compose.yml
├── .env.example
├── .gitignore
└── README.md
```

Use `compose.yml` as the primary Docker Compose filename unless the repository already follows another established convention.

Do not commit a real `.env` file containing credentials.

Create `.env.example` containing safe example values.

# Required Docker Services

The Compose stack must contain three primary services:

```text
guardlan-ui
guardlan-api
guardlan-database
```

The exact Compose service names should preferably remain short and usable as internal DNS names:

```yaml
services:
  ui:
  api:
  database:
```

Containers may use descriptive container names, but avoid relying on container names for service discovery.

Inside the Docker Compose network:

* The API must reach PostgreSQL through the hostname `database`.
* The UI must communicate with the API through an appropriate reverse-proxy or runtime configuration strategy.
* Containers must not use `localhost` to communicate with one another.

# 1. GuardLAN API Docker Setup

Inspect the ASP.NET Core solution and determine:

* The startup API project
* The target .NET version
* The solution file location
* Project references
* Worker projects
* Migration assembly
* Existing configuration files
* Existing database provider
* Existing exposed ports

Create a production-style, multi-stage Dockerfile inside `GuardLAN.API`.

The Dockerfile must:

1. Use the correct official .NET SDK image for building.
2. Use the matching ASP.NET runtime image for execution.
3. Copy project files in a layer-efficient order.
4. Restore dependencies before copying the complete source.
5. Build and publish the API in Release configuration.
6. Copy only published output into the runtime image.
7. Run the correct API assembly.
8. Expose the correct internal API port.
9. Avoid running development tooling in the final image.
10. Use a non-root user when practical and supported by the selected image.
11. Include an appropriate `.dockerignore`.

Example intent:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:<version> AS build
WORKDIR /src

# Copy solution and project files
# Restore dependencies
# Copy remaining source
# Publish API

FROM mcr.microsoft.com/dotnet/aspnet:<version> AS runtime
WORKDIR /app

# Copy published output
# Configure port
# Run API
```

Do not blindly use placeholder project names. Use the actual project and assembly names found in the repository.

## API Configuration

Configure the API through environment variables.

The PostgreSQL connection string must be supplied using the ASP.NET Core configuration convention:

```text
ConnectionStrings__DefaultConnection
```

Inside Docker, the connection string must use:

```text
Host=database
```

Example structure:

```text
Host=database;
Port=5432;
Database=guardlan;
Username=guardlan;
Password=<environment variable>;
```

Do not hard-code the database password in source code or `compose.yml`.

Set the required ASP.NET environment and HTTP port through Compose.

Example:

```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Development
  ASPNETCORE_HTTP_PORTS: 8080
```

Use the configuration style supported by the actual .NET version.

## API Health Check

Ensure the API exposes a health endpoint, preferably:

```text
GET /health
```

The endpoint should at minimum report whether the API process is running.

Where appropriate, include a PostgreSQL dependency check.

The endpoint must return a successful HTTP status when the application is healthy.

Add a Docker health check for the API.

Do not make the UI depend only on process startup if a proper API health check is available.

## Database Migration Strategy

Inspect how EF Core migrations currently work.

Document and implement one clear strategy.

Preferred options, in order:

### Option A: API-controlled migration on startup

Use this only if the existing architecture already supports safe automatic migrations for local development.

The API may apply pending migrations during startup when running in the local Docker environment.

This behavior should be development-specific and clearly documented.

### Option B: Dedicated migration command

Document a command such as:

```bash
docker compose exec api dotnet <migration-assembly>.dll
```

Only use this if the application contains a dedicated migration executable.

### Option C: EF Core tooling through a temporary SDK container

Use this only if required by the actual solution.

Do not invent a migration process that cannot work with the current repository.

Whichever approach is selected, document:

* How migrations are initially applied
* How new migrations are created
* How existing migrations are applied
* How database failures are diagnosed
* How the database is reset

The API must not begin serving database-dependent requests before PostgreSQL is reachable.

# 2. PostgreSQL Docker Setup

Use the official PostgreSQL image.

Choose a stable supported version compatible with the project.

The database service must define:

* Database name
* Database user
* Database password
* Persistent named volume
* Health check
* Restart policy

Example intent:

```yaml
database:
  image: postgres:<version>
  environment:
    POSTGRES_DB: ${POSTGRES_DB}
    POSTGRES_USER: ${POSTGRES_USER}
    POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
  volumes:
    - guardlan-database-data:/var/lib/postgresql/data
  healthcheck:
    test:
      - CMD-SHELL
      - pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}
```

Use a named Docker volume such as:

```yaml
volumes:
  guardlan-database-data:
```

Database data must survive:

```bash
docker compose down
```

Database data may be removed intentionally through:

```bash
docker compose down --volumes
```

Document this distinction clearly.

## Database Port Exposure

Decide whether PostgreSQL needs to be exposed to the host.

For local development, exposing the port may be useful for tools such as:

* JetBrains DataGrip
* DBeaver
* pgAdmin
* Visual Studio database tools

If exposed, make it configurable through `.env`.

Example:

```yaml
ports:
  - "${POSTGRES_HOST_PORT:-5432}:5432"
```

Clearly explain that the API inside Docker still connects through:

```text
database:5432
```

and not through the host-mapped port.

# 3. GuardLAN UI Docker Setup

Inspect the Angular workspace and determine:

* Angular version
* Node version requirements
* Package manager
* Application project name
* Build command
* Output directory
* Existing environment configuration
* Existing API URL strategy
* Whether server-side rendering is enabled
* Existing development proxy configuration

Create a multi-stage Dockerfile inside `GuardLAN.UI`.

For a standard Angular client application, use:

1. Node build stage
2. Nginx runtime stage

The Dockerfile must:

1. Use a Node version compatible with the Angular project.
2. Copy package manifest and lock file first.
3. Install dependencies using the lock-file-safe command.
4. Copy the remaining source.
5. Build the Angular application in the correct configuration.
6. Copy the generated browser files into an Nginx runtime image.
7. Include a custom Nginx configuration.
8. Support Angular client-side routing.
9. Proxy API calls to the API service.
10. Include an appropriate `.dockerignore`.

Use the correct package-manager command:

```text
npm ci
pnpm install --frozen-lockfile
yarn install --frozen-lockfile
```

Select the command based on the lock file actually present.

Do not delete or regenerate the existing lock file unless necessary.

## Nginx Configuration

Create an Nginx configuration for the Angular application.

It must:

* Serve Angular static files.
* Support client-side routing through `index.html`.
* Proxy API requests to the Compose service named `api`.
* Forward relevant proxy headers.
* Avoid exposing internal Docker hostnames to browser code.

Recommended routing:

```text
Browser request:
http://localhost:<ui-port>/api/devices

Nginx proxy:
http://api:8080/api/devices
```

Example intent:

```nginx
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    location /api/ {
        proxy_pass http://api:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

Adapt the configuration to the actual API route prefix.

This same-origin reverse-proxy approach is preferred because it avoids hard-coding Docker service names in the Angular application and reduces local CORS configuration.

## UI API Configuration

The Angular application should use a relative API base path where practical:

```text
/api
```

Do not configure the browser application to call:

```text
http://api:8080
```

The hostname `api` only exists inside Docker and cannot be resolved by the user’s browser.

If the project already uses Angular environments or runtime configuration, preserve the existing pattern while ensuring the Docker deployment works correctly.

## UI Health Check

Add a simple health check for the UI container.

It may verify that Nginx returns a successful response for:

```text
/
```

or a dedicated static health endpoint.

# 4. Docker Compose Configuration

Create one root-level `compose.yml`.

It must define:

* `database`
* `api`
* `ui`
* Named database volume
* Health checks
* Dependency startup order
* Restart policies
* Build contexts
* Host port mappings
* Environment variables

Expected dependency order:

```text
database becomes healthy
    |
api starts and becomes healthy
    |
ui starts
```

Use:

```yaml
depends_on:
  database:
    condition: service_healthy
```

for the API.

Where supported and useful, make the UI depend on API health.

Do not assume that basic `depends_on` means the dependency is ready.

## Example Compose Shape

Create the final file based on the actual repository, but it should conceptually resemble:

```yaml
name: guardlan

services:
  database:
    image: postgres:<version>
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - guardlan-database-data:/var/lib/postgresql/data
    ports:
      - "${POSTGRES_HOST_PORT:-5432}:5432"
    healthcheck:
      test:
        [
          "CMD-SHELL",
          "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"
        ]
      interval: 5s
      timeout: 5s
      retries: 10

  api:
    build:
      context: ./GuardLAN.API
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      ASPNETCORE_HTTP_PORTS: 8080
      ConnectionStrings__DefaultConnection: >-
        Host=database;
        Port=5432;
        Database=${POSTGRES_DB};
        Username=${POSTGRES_USER};
        Password=${POSTGRES_PASSWORD}
    depends_on:
      database:
        condition: service_healthy
    ports:
      - "${API_HOST_PORT:-8080}:8080"
    healthcheck:
      test:
        [
          "CMD",
          "curl",
          "--fail",
          "http://localhost:8080/health"
        ]
      interval: 10s
      timeout: 5s
      retries: 10

  ui:
    build:
      context: ./GuardLAN.UI
      dockerfile: Dockerfile
    restart: unless-stopped
    depends_on:
      api:
        condition: service_healthy
    ports:
      - "${UI_HOST_PORT:-4200}:80"

volumes:
  guardlan-database-data:
```

Do not copy this blindly.

Verify:

* Whether the API runtime image contains `curl`
* Whether another health-check mechanism is required
* Actual ports
* Actual API health path
* Actual PostgreSQL version
* Actual Angular output location
* Actual project and assembly names

If `curl` is unavailable in the runtime image, either:

* Install a minimal health-check dependency responsibly
* Use another available tool
* Use a TCP-based health check
* Use an internal application health-check mechanism

Do not leave a health check that always fails because its command does not exist.

# 5. Environment Variables

Create `.env.example`.

Include all required values:

```dotenv
POSTGRES_DB=guardlan
POSTGRES_USER=guardlan
POSTGRES_PASSWORD=change-me

POSTGRES_HOST_PORT=5432
API_HOST_PORT=8080
UI_HOST_PORT=4200

ASPNETCORE_ENVIRONMENT=Development
```

Add other required values discovered from the application configuration.

Examples may include:

* JWT settings
* CORS settings
* Allowed hosts
* Logging configuration
* Data-retention configuration
* Initial administrator configuration

Do not expose real secrets.

Document that the developer should copy:

```bash
cp .env.example .env
```

On PowerShell, also provide:

```powershell
Copy-Item .env.example .env
```

Ensure `.env` is ignored by Git.

# 6. Root README Documentation

Create or update the repository root `README.md`.

Preserve the existing GuardLAN project introduction and purpose where possible.

Add a clear section:

```text
Local Development with Docker
```

The documentation must include the complete first-run flow.

## Required Prerequisites

Document:

* Git
* Docker Desktop or Docker Engine
* Docker Compose v2
* Optional Node.js for non-Docker UI development
* Optional .NET SDK for non-Docker API development
* Optional PostgreSQL client

Include commands to verify installations:

```bash
git --version
docker --version
docker compose version
```

## Clone and Configure

Document:

```bash
git clone <repository-url>
cd GuardLAN
cp .env.example .env
```

Also include PowerShell equivalents where commands differ.

Explain which values in `.env` should be changed.

## First Start

Document:

```bash
docker compose up --build
```

Detached mode:

```bash
docker compose up --build -d
```

Explain what happens:

1. PostgreSQL image is downloaded.
2. API image is built.
3. UI image is built.
4. PostgreSQL starts.
5. API waits for PostgreSQL.
6. Database migrations are applied according to the selected strategy.
7. UI starts after the API becomes available.

## Access URLs

Document the final actual URLs, for example:

```text
GuardLAN UI:
http://localhost:4200

GuardLAN API:
http://localhost:8080

Swagger:
http://localhost:8080/swagger

Health:
http://localhost:8080/health

PostgreSQL:
localhost:5432
```

Only document URLs that actually work.

## Verify the Environment

Include commands:

```bash
docker compose ps
docker compose logs database
docker compose logs api
docker compose logs ui
```

Include a basic health test:

```bash
curl http://localhost:8080/health
```

Provide an equivalent browser or PowerShell check when useful.

## Stop the Environment

Document:

```bash
docker compose down
```

Explain that the named PostgreSQL volume remains.

## Restart the Environment

Document:

```bash
docker compose up -d
```

Explain when `--build` is required.

## Rebuild After Code Changes

Document:

```bash
docker compose up --build
```

For one service:

```bash
docker compose build api
docker compose up -d api
```

And:

```bash
docker compose build ui
docker compose up -d ui
```

## Reset Database

Document the destructive command:

```bash
docker compose down --volumes
docker compose up --build
```

Add a clear warning that this permanently removes local PostgreSQL data stored in the Compose volume.

## View Logs

Document:

```bash
docker compose logs -f
docker compose logs -f api
docker compose logs -f ui
docker compose logs -f database
```

## Enter Containers

Document useful commands:

```bash
docker compose exec api sh
docker compose exec ui sh
docker compose exec database psql \
  -U "$POSTGRES_USER" \
  -d "$POSTGRES_DB"
```

Adapt shell commands for the actual images.

## Common Problems

Include troubleshooting for:

### Port already in use

Explain how to change:

```dotenv
UI_HOST_PORT
API_HOST_PORT
POSTGRES_HOST_PORT
```

### API cannot connect to database

Check:

* Database health
* Connection string
* Service hostname
* Credentials
* Migration status

Commands:

```bash
docker compose ps
docker compose logs database
docker compose logs api
```

### UI cannot reach API

Check:

* Nginx proxy configuration
* API health
* `/api` base path
* Browser network tab
* UI logs
* API logs

### Database schema missing

Explain the migration command or startup behavior.

### Old images or cached builds

Document:

```bash
docker compose build --no-cache
docker compose up -d
```

### Full Docker cleanup

Document carefully:

```bash
docker compose down --volumes --remove-orphans
```

Do not recommend global Docker cleanup commands that could delete unrelated user projects unless clearly marked as optional and dangerous.

# 7. GuardLAN.API Documentation

Create or update a backend-specific README, preferably:

```text
GuardLAN.API/README.md
```

Document:

* Backend project purpose
* Solution and project structure
* Required .NET version
* How to run through Docker
* How to run without Docker
* Database configuration
* EF Core migration process
* Health endpoint
* Swagger
* Environment configuration
* Worker behavior
* Testing commands
* Architecture-document locations

## Running Without Docker

Document the actual commands after inspecting the solution.

Example intent:

```bash
dotnet restore
dotnet build
dotnet run --project <actual-api-project>
```

For local PostgreSQL, explain the required connection string.

Do not provide commands that use placeholder project names.

## Backend Tests

Document actual test commands:

```bash
dotnet test
```

or targeted project commands where required.

# 8. GuardLAN.UI Documentation

Create or update:

```text
GuardLAN.UI/README.md
```

Document:

* Angular project purpose
* Required Node.js version
* Package manager
* Dependency installation
* Development server
* Production build
* Unit tests
* Linting
* API proxy behavior
* Docker behavior
* Angular architecture-document locations

## Running Without Docker

Use the actual package manager.

Example intent:

```bash
npm ci
npm start
```

or:

```bash
npm run start
```

Document how the local Angular development server reaches the locally running API.

Prefer an Angular proxy configuration such as:

```text
/api -> http://localhost:8080
```

when the frontend runs outside Docker.

Do not require developers to hard-code local API URLs into source files.

## UI Tests

Document the actual available scripts:

```bash
npm test
npm run lint
npm run build
```

Only document scripts that exist or create the appropriate scripts if they are required by the project conventions.

# 9. Architecture Documentation Update

Update the existing architecture documentation where necessary.

The backend documentation should explain:

* Docker does not change the Unit of Work, repository or DTO architecture.
* Database connectivity comes from configuration.
* Background workers run within the selected backend deployment model.
* Migrations and startup behavior must remain explicit.

The frontend documentation should explain:

* Production Docker builds produce static Angular files.
* Nginx serves the UI.
* `/api` requests are reverse-proxied to the API.
* Browser code must never reference Docker-only hostnames.
* Docker deployment must preserve the one-request-per-view architecture.

Create a dedicated document if helpful:

```text
docs/DOCKER.md
```

It may contain the detailed infrastructure explanation while the root README contains the essential first-run steps.

# 10. Development and Production Separation

The first objective is a reliable local environment.

Do not falsely describe the local Compose file as production-ready.

Clearly document which concerns would still be required for production, such as:

* Managed secrets
* HTTPS termination
* Trusted certificates
* Authentication configuration
* Database backups
* External persistent storage
* Reverse proxy
* Rate limiting
* Monitoring
* Log collection
* Database high availability
* Container image registry
* Automated deployment
* Network exposure restrictions

Do not over-engineer a full production platform unless the repository already requires it.

# 11. Docker Ignore Files

Create:

```text
GuardLAN.API/.dockerignore
GuardLAN.UI/.dockerignore
```

The API ignore file should exclude where appropriate:

```text
**/bin
**/obj
.git
.vs
.idea
TestResults
coverage
.env
```

The UI ignore file should exclude where appropriate:

```text
node_modules
dist
.angular
coverage
.git
.idea
.vscode
.env
```

Do not exclude files required for the Docker build.

# 12. Security Requirements

* Do not commit passwords.
* Do not place secrets in Dockerfiles.
* Do not place secrets in Angular build output.
* Do not expose PostgreSQL publicly beyond local development needs.
* Do not disable TLS validation in application code.
* Do not configure permissive CORS unnecessarily.
* Prefer same-origin `/api` proxying through Nginx.
* Ensure Swagger exposure matches the selected environment.
* Do not run containers as root where a practical supported alternative exists.
* Do not log connection strings containing passwords.
* Do not store production secrets in `.env`.

# 13. Validation Requirements

After implementation, perform or document validation for all of the following:

## Configuration validation

```bash
docker compose config
```

This command must complete successfully.

## Image build

```bash
docker compose build
```

All custom images must build successfully.

## First startup

```bash
docker compose up -d
```

All services must start.

## Service health

```bash
docker compose ps
```

The database and API must become healthy.

The UI must be reachable.

## API validation

Verify:

* Health endpoint
* Swagger
* Database connectivity
* At least one database-backed endpoint where available

## UI validation

Verify:

* Angular application loads.
* Direct navigation to a client route works.
* Refreshing a client route does not return an Nginx 404.
* `/api` requests reach the backend.
* The browser does not attempt to resolve the Docker hostname `api`.

## Persistence validation

1. Create or confirm database data.
2. Run:

```bash
docker compose down
docker compose up -d
```

3. Confirm the data still exists.

## Reset validation

Run:

```bash
docker compose down --volumes
```

Confirm the database volume is removed.

# 14. Implementation Quality Rules

* Inspect existing files before adding alternatives.
* Preserve current naming conventions.
* Do not create duplicate configuration systems.
* Do not hard-code repository-specific assumptions without verification.
* Keep Docker configuration readable.
* Add comments only where they clarify non-obvious decisions.
* Keep the Compose file focused on orchestration.
* Keep application logic out of Docker scripts.
* Use official base images.
* Pin meaningful major or minor image versions.
* Avoid using `latest`.
* Ensure documentation matches the implemented commands.
* Do not leave placeholder paths or assembly names.
* Do not document commands that have not been verified against the project structure.
* Preserve the existing backend and frontend architecture.
* Do not change application architecture solely to make Docker easier.
* Do not introduce Kubernetes.
* Do not introduce unnecessary infrastructure services.
* Do not add Redis unless the existing application requires it.
* Do not add pgAdmin as a mandatory service.
* Do not require developers to start UI and API manually when using Compose.

# 15. Expected Deliverables

At completion, provide:

1. Root `compose.yml`
2. Root `.env.example`
3. Updated root `.gitignore`
4. `GuardLAN.API/Dockerfile`
5. `GuardLAN.API/.dockerignore`
6. `GuardLAN.API/README.md`
7. `GuardLAN.UI/Dockerfile`
8. `GuardLAN.UI/.dockerignore`
9. GuardLAN UI Nginx configuration
10. `GuardLAN.UI/README.md`
11. Updated root `README.md`
12. Optional `docs/DOCKER.md`
13. Required API health-check implementation
14. Required Angular proxy or runtime API configuration
15. Verified database migration strategy
16. A final summary of created and modified files
17. A final list of commands required for the first run

# 16. Final First-Run Experience

The documented first-time workflow should be approximately:

```bash
git clone <repository-url>
cd GuardLAN
cp .env.example .env
docker compose up --build
```

The developer should then be able to open:

```text
UI:
http://localhost:4200

API:
http://localhost:8080

Swagger:
http://localhost:8080/swagger

Health:
http://localhost:8080/health
```

The exact ports and paths must match the final implementation.

The developer must not need to manually start the UI, API or PostgreSQL separately.

# 17. Final Response Requirements

After implementing the setup, report:

* Which files were created
* Which files were modified
* Which ports are used
* Which PostgreSQL version is used
* How migrations are handled
* How the UI reaches the API
* How environment variables are configured
* The exact first-run command
* The exact stop command
* The exact reset command
* Any unresolved issue or assumption

Be explicit about anything that could not be tested or verified.

Do not claim that the environment works unless the relevant configuration and build commands were actually validated.
