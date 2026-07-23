# Backend Architecture and Endpoint Development Guidelines

## 1. Purpose

This document defines the required backend architecture for the GuardLAN network-monitoring application.

Every new API endpoint must follow the same structure and separation of responsibilities.

The backend uses:

* ASP.NET Core Web API
* Entity Framework Core
* Unit of Work pattern
* Generic repositories
* Entity-specific repositories
* DTOs for API communication
* Service layer for application logic
* Dependency injection
* Async database operations
* Centralized exception handling

The main objective is to keep:

* Controllers or endpoints thin
* Business logic outside the API layer
* Database access isolated in repositories
* EF Core entities hidden from external consumers
* Code testable and maintainable

---

# 2. Required Request Flow

Every API request should follow this flow:

```text
HTTP Request
    |
API Controller / Endpoint
    |
Application Service
    |
Unit of Work
    |
Generic or Entity Repository
    |
Entity Framework Core
    |
Database
```

The response follows the reverse direction:

```text
Database Entity
    |
Application Service
    |
DTO Mapping
    |
API Response DTO
    |
HTTP Response
```

The API layer must never directly access `DbContext`.

The API layer must never return EF Core entities.

---

# 3. Solution Structure

The preferred solution structure is:

```text
GuardLAN.sln

src/
  GuardLAN.Api/
  GuardLAN.Application/
  GuardLAN.Domain/
  GuardLAN.Infrastructure/

tests/
  GuardLAN.UnitTests/
  GuardLAN.IntegrationTests/
```

## GuardLAN.Api

Contains:

* Controllers
* Endpoint definitions
* Middleware
* API configuration
* Authentication and authorization configuration
* Dependency injection registration
* Swagger configuration
* Request-specific API models when necessary

The API project must not contain business logic or direct database access.

## GuardLAN.Application

Contains:

* Application services
* Service interfaces
* DTOs
* Mapping logic
* Validation
* Application-specific exceptions
* Query and command models if introduced later

The Application project coordinates business operations.

## GuardLAN.Domain

Contains:

* Entities
* Enums
* Value objects
* Domain rules
* Repository interfaces
* Unit of Work interface

The Domain project must not depend on Infrastructure or API projects.

## GuardLAN.Infrastructure

Contains:

* EF Core `DbContext`
* Entity configurations
* Repository implementations
* Unit of Work implementation
* Database migrations
* External integrations
* Scanner implementations
* Pi-hole integrations
* Zeek integrations
* Suricata integrations

Infrastructure implements interfaces defined in the Domain or Application layers.

---

# 4. Feature-Based Folder Structure

Within each project, code should be grouped by feature where practical.

Example:

```text
GuardLAN.Application/
  Devices/
    DTOs/
      DeviceDto.cs
      DeviceDetailsDto.cs
      CreateDeviceDto.cs
      UpdateDeviceDto.cs
      DeviceQueryDto.cs

    Interfaces/
      IDeviceService.cs

    Services/
      DeviceService.cs

    Validators/
      CreateDeviceDtoValidator.cs
      UpdateDeviceDtoValidator.cs

    Mappings/
      DeviceMappings.cs
```

```text
GuardLAN.Domain/
  Devices/
    Entities/
      NetworkDevice.cs

    Enums/
      DeviceType.cs
      DeviceStatus.cs

    Repositories/
      IDeviceRepository.cs
```

```text
GuardLAN.Infrastructure/
  Persistence/
    GuardLANDbContext.cs
    UnitOfWork.cs

    Configurations/
      NetworkDeviceConfiguration.cs

    Repositories/
      GenericRepository.cs
      DeviceRepository.cs
```

```text
GuardLAN.Api/
  Controllers/
    DevicesController.cs
```

Feature folders are preferred over one large global folder containing all services, DTOs or repositories.

---

# 5. Entity Rules

Entities represent the persisted domain model.

Example:

```csharp
public sealed class NetworkDevice
{
    public Guid Id { get; set; }

    public required string IpAddress { get; set; }

    public required string MacAddress { get; set; }

    public string? Hostname { get; set; }

    public string? DisplayName { get; set; }

    public string? Vendor { get; set; }

    public DeviceType DeviceType { get; set; }

    public bool IsTrusted { get; set; }

    public bool IsOnline { get; set; }

    public DateTime FirstSeenUtc { get; set; }

    public DateTime LastSeenUtc { get; set; }
}
```

Rules:

* Entities must not be exposed through API responses.
* Entities must not be used directly as controller input.
* Entities should contain persisted state and domain-specific behavior.
* Entity properties should use nullable reference types correctly.
* All stored timestamps should use UTC.
* Entity relationships must be configured through EF Core configurations.
* Avoid placing HTTP-specific concerns inside entities.
* Avoid placing DTO mapping logic inside entities.

---

# 6. DTO Rules

DTOs define the data contract between the API and its consumers.

DTOs are mandatory for:

* Request bodies
* API responses
* List projections
* Details responses
* Create operations
* Update operations
* Filtering and pagination

## Response DTO

```csharp
public sealed record DeviceDto(
    Guid Id,
    string IpAddress,
    string MacAddress,
    string? Hostname,
    string? DisplayName,
    string? Vendor,
    DeviceType DeviceType,
    bool IsTrusted,
    bool IsOnline,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc);
```

## Create DTO

```csharp
public sealed record CreateDeviceDto(
    string IpAddress,
    string MacAddress,
    string? Hostname,
    string? DisplayName,
    DeviceType DeviceType);
```

## Update DTO

```csharp
public sealed record UpdateDeviceDto(
    string? DisplayName,
    DeviceType DeviceType,
    bool IsTrusted);
```

## Query DTO

```csharp
public sealed class DeviceQueryDto
{
    public string? Search { get; init; }

    public bool? IsOnline { get; init; }

    public bool? IsTrusted { get; init; }

    public DeviceType? DeviceType { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
```

Rules:

* Never return entities from endpoints.
* Never accept entities as endpoint input.
* Request and response DTOs should be separate when their purposes differ.
* Do not reuse a response DTO as an update model merely because the properties currently match.
* DTO property names should form a stable external API contract.
* Sensitive internal properties must not be included.
* DTOs should not contain EF Core navigation entities.
* DTOs should be small and endpoint-specific where necessary.
* List DTOs should not automatically include every detail field.

---

# 7. Generic Repository

The generic repository provides common persistence operations.

```csharp
public interface IGenericRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
```

Implementation:

```csharp
public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class
{
    protected readonly GuardLANDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public GenericRepository(GuardLANDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }
}
```

## Generic Repository Rules

The generic repository should contain only operations that are truly generic.

Appropriate examples:

* Get by ID
* Get all
* Add
* Update
* Remove
* Check whether an entity exists

Inappropriate examples:

```csharp
GetOnlineDevicesAsync();
GetBlockedDnsQueriesAsync();
GetUnresolvedCriticalAlertsAsync();
```

Those belong in entity-specific repositories.

The generic repository must not grow into a large universal query service.

---

# 8. Entity-Specific Repositories

Every entity may have its own repository when it requires specialized queries.

Example:

```csharp
public interface IDeviceRepository
    : IGenericRepository<NetworkDevice>
{
    Task<NetworkDevice?> GetByMacAddressAsync(
        string macAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDevice>> GetOnlineDevicesAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<NetworkDevice>> SearchAsync(
        DeviceQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByMacAddressAsync(
        string macAddress,
        CancellationToken cancellationToken = default);
}
```

Implementation:

```csharp
public sealed class DeviceRepository
    : GenericRepository<NetworkDevice>,
      IDeviceRepository
{
    public DeviceRepository(GuardLANDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<NetworkDevice?> GetByMacAddressAsync(
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(
                device => device.MacAddress == macAddress,
                cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkDevice>> GetOnlineDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(device => device.IsOnline)
            .OrderBy(device => device.DisplayName ?? device.Hostname)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByMacAddressAsync(
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(
                device => device.MacAddress == macAddress,
                cancellationToken);
    }
}
```

Rules:

* Specialized queries belong in the repository responsible for that entity.
* Query naming must describe intent clearly.
* Read-only queries must generally use `AsNoTracking()`.
* Repository methods should not return DTOs unless projection repositories are intentionally introduced.
* Repository methods must not return `IQueryable` to higher layers.
* EF Core expressions must stay inside Infrastructure.
* Avoid repository methods that represent business workflows.
* Repositories access data; services coordinate behavior.

---

# 9. Unit of Work

The Unit of Work provides access to repositories and controls transaction persistence.

Interface:

```csharp
public interface IUnitOfWork
{
    IDeviceRepository Devices { get; }

    IDnsQueryRepository DnsQueries { get; }

    INetworkConnectionRepository NetworkConnections { get; }

    ISecurityAlertRepository SecurityAlerts { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(
        CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default);
}
```

Implementation:

```csharp
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly GuardLANDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        GuardLANDbContext dbContext,
        IDeviceRepository devices,
        IDnsQueryRepository dnsQueries,
        INetworkConnectionRepository networkConnections,
        ISecurityAlertRepository securityAlerts)
    {
        _dbContext = dbContext;
        Devices = devices;
        DnsQueries = dnsQueries;
        NetworkConnections = networkConnections;
        SecurityAlerts = securityAlerts;
    }

    public IDeviceRepository Devices { get; }

    public IDnsQueryRepository DnsQueries { get; }

    public INetworkConnectionRepository NetworkConnections { get; }

    public ISecurityAlertRepository SecurityAlerts { get; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A database transaction is already active.");
        }

        _transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "No active database transaction exists.");
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
```

## Unit of Work Rules

* Repositories must not call `SaveChangesAsync()` themselves.
* The service decides when a complete operation is persisted.
* One business operation should normally produce one `SaveChangesAsync()` call.
* Explicit transactions are required only when multiple persistence steps must succeed or fail together.
* Avoid creating a transaction for every simple CRUD operation because EF Core already wraps a single `SaveChangesAsync()` operation in a transaction where required.
* Controllers must not call repositories directly.
* Controllers must not call `SaveChangesAsync()` directly.

---

# 10. Service Layer

Application services contain endpoint-related application logic.

Interface:

```csharp
public interface IDeviceService
{
    Task<PagedResult<DeviceDto>> GetDevicesAsync(
        DeviceQueryDto query,
        CancellationToken cancellationToken = default);

    Task<DeviceDetailsDto> GetDeviceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DeviceDto> CreateDeviceAsync(
        CreateDeviceDto dto,
        CancellationToken cancellationToken = default);

    Task<DeviceDto> UpdateDeviceAsync(
        Guid id,
        UpdateDeviceDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteDeviceAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
```

Implementation:

```csharp
public sealed class DeviceService : IDeviceService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeviceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeviceDetailsDto> GetDeviceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var device = await _unitOfWork.Devices
            .GetByIdAsync(id, cancellationToken);

        if (device is null)
        {
            throw new NotFoundException(
                $"Device with ID '{id}' was not found.");
        }

        return device.ToDetailsDto();
    }

    public async Task<DeviceDto> CreateDeviceAsync(
        CreateDeviceDto dto,
        CancellationToken cancellationToken = default)
    {
        var macAddress = NormalizeMacAddress(dto.MacAddress);

        var alreadyExists = await _unitOfWork.Devices
            .ExistsByMacAddressAsync(macAddress, cancellationToken);

        if (alreadyExists)
        {
            throw new ConflictException(
                $"A device with MAC address '{macAddress}' already exists.");
        }

        var entity = new NetworkDevice
        {
            Id = Guid.NewGuid(),
            IpAddress = dto.IpAddress.Trim(),
            MacAddress = macAddress,
            Hostname = dto.Hostname?.Trim(),
            DisplayName = dto.DisplayName?.Trim(),
            DeviceType = dto.DeviceType,
            FirstSeenUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
            IsOnline = true,
            IsTrusted = false
        };

        await _unitOfWork.Devices
            .AddAsync(entity, cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }

    public async Task<DeviceDto> UpdateDeviceAsync(
        Guid id,
        UpdateDeviceDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Devices
            .GetByIdAsync(id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException(
                $"Device with ID '{id}' was not found.");
        }

        entity.DisplayName = dto.DisplayName?.Trim();
        entity.DeviceType = dto.DeviceType;
        entity.IsTrusted = dto.IsTrusted;

        _unitOfWork.Devices.Update(entity);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }

    public async Task DeleteDeviceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Devices
            .GetByIdAsync(id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException(
                $"Device with ID '{id}' was not found.");
        }

        _unitOfWork.Devices.Remove(entity);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeMacAddress(string macAddress)
    {
        return macAddress
            .Trim()
            .Replace("-", ":")
            .ToUpperInvariant();
    }
}
```

## Service Layer Rules

The service layer is responsible for:

* Coordinating repositories
* Applying business rules
* Validating existence and uniqueness
* Mapping entities to DTOs
* Creating or updating entities
* Calling `SaveChangesAsync()`
* Managing explicit transactions
* Throwing application-specific exceptions
* Coordinating external services

The service layer must not:

* Return EF Core entities
* Contain HTTP status-code logic
* Depend directly on controllers
* Return `IActionResult`
* Access `HttpContext`
* Use EF Core `DbContext` directly
* Construct raw HTTP responses

---

# 11. DTO Mapping

Mapping should be explicit and easy to inspect.

For simple mappings, use extension methods.

```csharp
public static class DeviceMappings
{
    public static DeviceDto ToDto(this NetworkDevice entity)
    {
        return new DeviceDto(
            entity.Id,
            entity.IpAddress,
            entity.MacAddress,
            entity.Hostname,
            entity.DisplayName,
            entity.Vendor,
            entity.DeviceType,
            entity.IsTrusted,
            entity.IsOnline,
            entity.FirstSeenUtc,
            entity.LastSeenUtc);
    }

    public static DeviceDetailsDto ToDetailsDto(
        this NetworkDevice entity)
    {
        return new DeviceDetailsDto(
            entity.Id,
            entity.IpAddress,
            entity.MacAddress,
            entity.Hostname,
            entity.DisplayName,
            entity.Vendor,
            entity.DeviceType,
            entity.IsTrusted,
            entity.IsOnline,
            entity.FirstSeenUtc,
            entity.LastSeenUtc);
    }
}
```

Rules:

* Mapping must not happen in controllers.
* Mapping must not mutate the entity.
* Mapping methods should be grouped by feature.
* Avoid reflection-based mapping unless it provides a clear benefit.
* Explicit mapping is preferred because it makes API contract changes visible during review.
* Large collection endpoints should preferably use database projection when performance becomes relevant.

Example projection:

```csharp
var devices = await DbSet
    .AsNoTracking()
    .Select(device => new DeviceListProjection
    {
        Id = device.Id,
        DisplayName = device.DisplayName,
        IpAddress = device.IpAddress,
        MacAddress = device.MacAddress,
        IsOnline = device.IsOnline
    })
    .ToListAsync(cancellationToken);
```

A projection type is internal and must not automatically become the external API DTO.

---

# 12. Controller and Endpoint Rules

Controllers must be thin.

A controller is responsible for:

* Receiving HTTP requests
* Binding route, query and body data
* Calling the appropriate service method
* Returning the correct HTTP result
* Supplying cancellation tokens
* Applying authorization attributes
* Declaring response metadata

Example:

```csharp
[ApiController]
[Route("api/devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResult<DeviceDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DeviceDto>>> GetDevices(
        [FromQuery] DeviceQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _deviceService
            .GetDevicesAsync(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(DeviceDetailsDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDetailsDto>> GetDevice(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _deviceService
            .GetDeviceAsync(id, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(DeviceDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceDto>> CreateDevice(
        [FromBody] CreateDeviceDto dto,
        CancellationToken cancellationToken)
    {
        var createdDevice = await _deviceService
            .CreateDeviceAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetDevice),
            new { id = createdDevice.Id },
            createdDevice);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(DeviceDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> UpdateDevice(
        Guid id,
        [FromBody] UpdateDeviceDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _deviceService
            .UpdateDeviceAsync(id, dto, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDevice(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _deviceService
            .DeleteDeviceAsync(id, cancellationToken);

        return NoContent();
    }
}
```

## Controller Rules

Controllers must not:

```csharp
// Forbidden
_dbContext.NetworkDevices.ToListAsync();

// Forbidden
_unitOfWork.Devices.GetAllAsync();

// Forbidden
entity.DisplayName = dto.DisplayName;

// Forbidden
return Ok(entity);

// Forbidden
catch (Exception exception)
{
    return StatusCode(500);
}
```

All data access and application logic must go through the service layer.

---

# 13. Validation

Request validation must happen before application logic is executed.

FluentValidation may be used.

Example:

```csharp
public sealed class CreateDeviceDtoValidator
    : AbstractValidator<CreateDeviceDto>
{
    public CreateDeviceDtoValidator()
    {
        RuleFor(dto => dto.IpAddress)
            .NotEmpty()
            .Must(BeValidIpAddress)
            .WithMessage("A valid IP address is required.");

        RuleFor(dto => dto.MacAddress)
            .NotEmpty()
            .Matches(
                "^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")
            .WithMessage("A valid MAC address is required.");

        RuleFor(dto => dto.DisplayName)
            .MaximumLength(100);

        RuleFor(dto => dto.Hostname)
            .MaximumLength(255);
    }

    private static bool BeValidIpAddress(string value)
    {
        return IPAddress.TryParse(value, out _);
    }
}
```

Rules:

* Format and required-field validation belongs in DTO validators.
* Database uniqueness checks belong in services or repositories.
* Business-rule validation belongs in services or domain logic.
* Do not query the database from a basic synchronous validator.
* Validation errors should use the standard `ValidationProblemDetails` response.

---

# 14. Exception Handling

Endpoints must not repeat `try/catch` blocks for known application errors.

Use centralized exception handling.

Recommended exceptions:

```text
NotFoundException
ValidationException
ConflictException
ForbiddenException
UnauthorizedException
BusinessRuleException
ExternalServiceException
```

Example handler behavior:

```text
NotFoundException       -> 404 Not Found
ValidationException     -> 400 Bad Request
ConflictException       -> 409 Conflict
ForbiddenException      -> 403 Forbidden
UnauthorizedException   -> 401 Unauthorized
BusinessRuleException   -> 422 Unprocessable Entity
Unknown exception       -> 500 Internal Server Error
```

Responses should use RFC-compatible `ProblemDetails`.

Example:

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Resource not found",
  "status": 404,
  "detail": "Device with ID '...' was not found.",
  "traceId": "..."
}
```

Internal exception details and stack traces must not be returned to clients.

---

# 15. Dependency Injection

Repositories, Unit of Work and services must be registered with scoped lifetime.

```csharp
services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

services.AddScoped<IDeviceRepository, DeviceRepository>();
services.AddScoped<IDnsQueryRepository, DnsQueryRepository>();
services.AddScoped<INetworkConnectionRepository, NetworkConnectionRepository>();
services.AddScoped<ISecurityAlertRepository, SecurityAlertRepository>();

services.AddScoped<IUnitOfWork, UnitOfWork>();

services.AddScoped<IDeviceService, DeviceService>();
services.AddScoped<IDnsQueryService, DnsQueryService>();
services.AddScoped<INetworkConnectionService, NetworkConnectionService>();
services.AddScoped<ISecurityAlertService, SecurityAlertService>();
```

Rules:

* `DbContext` remains scoped.
* Repositories remain scoped.
* Unit of Work remains scoped.
* Application services remain scoped.
* Stateless utility services may be singleton when safe.
* Hosted workers must create scopes before resolving scoped services.

---

# 16. Read and Write Query Rules

## Read Operations

Read-only queries should use:

```csharp
.AsNoTracking()
```

Read operations should:

* Select only required fields where practical
* Support pagination for potentially large collections
* Avoid loading unnecessary relationships
* Avoid uncontrolled `Include` chains
* Avoid returning entire tables
* Use deterministic ordering

## Write Operations

Write operations should:

* Load the existing entity when updating
* Change only allowed fields
* Preserve protected system-controlled properties
* Call `SaveChangesAsync()` once after all changes are prepared
* Handle concurrency where required

The endpoint must not allow clients to modify fields such as:

```text
Id
FirstSeenUtc
LastSeenUtc
CreatedUtc
UpdatedUtc
System-generated vendor information
Scanner-controlled online state
Internal alert metadata
```

unless explicitly required by the use case.

---

# 17. Pagination

Every endpoint that can return a growing collection must support pagination.

Generic result:

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
```

Rules:

* Default page size: `25`
* Maximum page size: `100`
* Page numbering starts at `1`
* The query must include a deterministic order
* Total count must be calculated separately when required
* Do not return unlimited DNS queries, connections or alerts

Example:

```text
GET /api/devices?page=1&pageSize=25
GET /api/dns-queries?page=2&pageSize=50
GET /api/alerts?severity=Critical&page=1&pageSize=25
```

---

# 18. Cancellation Tokens

Every asynchronous endpoint, service method and repository method must accept a `CancellationToken`.

Correct:

```csharp
public async Task<DeviceDto> GetDeviceAsync(
    Guid id,
    CancellationToken cancellationToken)
```

The cancellation token must be passed through every layer:

```text
Controller
    -> Service
        -> Repository
            -> EF Core
```

Do not replace the provided token with `CancellationToken.None`.

---

# 19. Naming Conventions

Use clear names that describe intent.

Preferred:

```text
GetDeviceAsync
GetOnlineDevicesAsync
GetByMacAddressAsync
ExistsByMacAddressAsync
CreateDeviceAsync
ResolveAlertAsync
GetRecentDnsQueriesAsync
```

Avoid:

```text
Handle
Process
DoWork
GetData
UpdateData
Execute
ManageDevice
```

Suffixes:

```text
Dto         External or application data contract
Entity      Usually unnecessary in class names
Repository  Data-access abstraction
Service     Application operation coordinator
Validator   DTO validation
Query       Internal search/filter criteria
Options     Configuration binding model
Result      Operation or query result
```

---

# 20. Date and Time Rules

* Store timestamps in UTC.
* Use `DateTime.UtcNow` or an injected time provider.
* Prefer `TimeProvider` for testable code.
* API timestamps must clearly represent UTC.
* Do not store local machine time.
* Convert to local time only in the frontend.

Preferred:

```csharp
public sealed class DeviceService
{
    private readonly TimeProvider _timeProvider;

    public DeviceService(
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    private DateTime GetUtcNow()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }
}
```

---

# 21. Logging Rules

Use structured logging.

Correct:

```csharp
_logger.LogInformation(
    "Device {DeviceId} was marked as trusted",
    deviceId);
```

Incorrect:

```csharp
_logger.LogInformation(
    $"Device {deviceId} was marked as trusted");
```

Log:

* Important state changes
* Background scan summaries
* External-service failures
* Security alert creation
* Failed authentication or authorization
* Unexpected exceptions

Do not log:

* Passwords
* Authentication tokens
* API secrets
* Complete sensitive request bodies
* Raw packet content by default
* Private user data without a clear need

---

# 22. API Endpoint Creation Checklist

Before implementing a new endpoint, the agent must answer:

## Contract

* What HTTP method is required?
* What route is required?
* What request DTO is required?
* What response DTO is required?
* Which status codes can be returned?
* Does the result require pagination?
* Does the endpoint require authorization?

## Domain

* Which entity or entities are involved?
* Is an entity-specific repository method required?
* Is there already a suitable repository method?
* What business rules must be enforced?
* Which fields are controlled by the system?
* Is an explicit transaction required?

## Implementation

* Create or reuse request DTO.
* Create or reuse response DTO.
* Add validation.
* Add repository interface method when required.
* Implement the repository method in Infrastructure.
* Add or update service interface.
* Implement application logic in the service.
* Map entities to DTOs.
* Call `SaveChangesAsync()` in the service.
* Add the thin endpoint or controller action.
* Pass the cancellation token through every layer.
* Add response-type documentation.
* Add unit tests.
* Add integration tests where appropriate.

---

# 23. Definition of Done for a New Endpoint

A new endpoint is complete only when:

* No EF Core entity is exposed through the API.
* No request directly binds to an entity.
* The controller contains no business logic.
* The controller does not access repositories directly.
* The controller does not access `DbContext`.
* The service contains the application logic.
* Repositories contain only data-access logic.
* The Unit of Work controls persistence.
* `SaveChangesAsync()` is not called from repositories.
* All async methods accept cancellation tokens.
* Read queries use `AsNoTracking()` where appropriate.
* Collection endpoints support pagination.
* Input DTOs are validated.
* Known errors use centralized exception handling.
* API responses use suitable status codes.
* DTO mappings are explicit.
* Dependency injection registration is updated.
* Unit tests cover important service behavior.
* Integration tests cover endpoint contracts where practical.
* Swagger correctly describes the endpoint.
* Logging does not expose sensitive data.

---

# 24. Forbidden Implementations

The following patterns must not be introduced.

## Direct DbContext Access in Controllers

```csharp
[HttpGet]
public async Task<IActionResult> GetDevices()
{
    var devices = await _dbContext.NetworkDevices.ToListAsync();

    return Ok(devices);
}
```

## Returning Entities

```csharp
public async Task<NetworkDevice> GetDeviceAsync(Guid id)
```

## Accepting Entities as Request Models

```csharp
[HttpPost]
public async Task<IActionResult> Create(
    NetworkDevice entity)
```

## Repository Saving Changes

```csharp
public async Task AddAsync(NetworkDevice device)
{
    await _dbSet.AddAsync(device);
    await _dbContext.SaveChangesAsync();
}
```

## Business Logic in Controllers

```csharp
if (device.IsTrusted && device.DeviceType == DeviceType.Unknown)
{
    device.DeviceType = DeviceType.Computer;
}
```

## Returning IQueryable

```csharp
public IQueryable<NetworkDevice> GetDevices()
{
    return DbSet;
}
```

## Generic Repository with Feature-Specific Methods

```csharp
public interface IGenericRepository<TEntity>
{
    Task<IReadOnlyList<TEntity>> GetOnlineDevicesAsync();
    Task<IReadOnlyList<TEntity>> GetCriticalAlertsAsync();
}
```

---

# 25. Standard Endpoint Example

For a new endpoint such as:

```text
PATCH /api/devices/{id}/trust
```

The required implementation is:

## Request DTO

```csharp
public sealed record SetDeviceTrustDto(bool IsTrusted);
```

## Service Interface

```csharp
Task<DeviceDto> SetTrustAsync(
    Guid id,
    SetDeviceTrustDto dto,
    CancellationToken cancellationToken = default);
```

## Service Implementation

```csharp
public async Task<DeviceDto> SetTrustAsync(
    Guid id,
    SetDeviceTrustDto dto,
    CancellationToken cancellationToken = default)
{
    var device = await _unitOfWork.Devices
        .GetByIdAsync(id, cancellationToken);

    if (device is null)
    {
        throw new NotFoundException(
            $"Device with ID '{id}' was not found.");
    }

    device.IsTrusted = dto.IsTrusted;

    _unitOfWork.Devices.Update(device);

    await _unitOfWork
        .SaveChangesAsync(cancellationToken);

    return device.ToDto();
}
```

## Controller Action

```csharp
[HttpPatch("{id:guid}/trust")]
[ProducesResponseType(
    typeof(DeviceDto),
    StatusCodes.Status200OK)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
public async Task<ActionResult<DeviceDto>> SetTrust(
    Guid id,
    [FromBody] SetDeviceTrustDto dto,
    CancellationToken cancellationToken)
{
    var result = await _deviceService
        .SetTrustAsync(id, dto, cancellationToken);

    return Ok(result);
}
```

The controller only handles the HTTP contract.

The service handles the operation.

The repository handles database access.

The Unit of Work persists the completed operation.

---

# 26. Instructions for Coding Agents

Whenever creating or modifying an endpoint:

1. Inspect the existing feature structure before adding files.
2. Reuse existing DTOs only when their semantic purpose matches.
3. Do not expose entities.
4. Do not bypass the service layer.
5. Do not access `DbContext` outside Infrastructure.
6. Use the Unit of Work for repository access and persistence.
7. Add specialized repository methods only when the query is entity-specific.
8. Keep generic repository methods truly generic.
9. Pass cancellation tokens through all async calls.
10. Use `AsNoTracking()` for read-only queries.
11. Use explicit DTO mapping.
12. Use centralized exception handling.
13. Add validation for every external request.
14. Add pagination to growing collections.
15. Add or update tests.
16. Preserve existing architecture and naming conventions.
17. Do not introduce alternative patterns without explicit approval.
18. Do not introduce MediatR, CQRS or direct minimal-endpoint database access unless the architecture decision is intentionally changed.
19. Do not call `SaveChangesAsync()` inside repositories.
20. Do not place business logic in controllers.

This architecture is the default and must be followed consistently for all new backend functionality.

The document is strict enough to use directly as an `AGENTS.md`, `BACKEND_ARCHITECTURE.md` or repository instruction file.
