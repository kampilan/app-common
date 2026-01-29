# AppCommon Class Library - Product Requirements Document

## Project Overview

AppCommon is a modular class library ecosystem designed for code reuse across Claude-built applications. The libraries are published to GitHub NuGet repository for easy consumption.

### Goals

- Provide reusable utilities, extensions, and patterns
- Isolate dependencies to minimize package bloat
- Maintain consistent coding standards across projects
- Enable rapid development of new applications

## Module Descriptions

### AppCommon.Core

Base utilities and extensions with minimal dependencies.

**Provides:**
- String extensions
- Collection extensions
- Guard clauses via CommunityToolkit.Diagnostics
- FluentValidation integration
- Common abstractions and interfaces
- Mediator pattern implementation
- Logging utilities
- Base entity types and audit infrastructure (IEntity, IRootEntity, IAggregateChild, AuditJournal, AuditJournalExtensions)

**Dependencies:** CommunityToolkit.Diagnostics, FluentValidation, Microsoft.Extensions.* abstractions, Ulid

### AppCommon.Aws

AWS SDK integrations and service utilities.

**Provides:**
- S3 client wrappers
- Secrets Manager helpers
- Parameter Store utilities
- DynamoDB helpers
- SQS utilities

**Dependencies:** AppCommon.Core, AWSSDK packages

### AppCommon.Persistence

Provider-agnostic database and data access patterns.

**Provides:**
- Repository pattern implementations
- Unit of Work pattern
- EF Core utilities and extensions
- Connection management
- Automatic audit logging via EF Core interceptor

**Dependencies:** AppCommon.Core, Entity Framework Core (provider-agnostic), Ulid

Note: This library does not include any database provider packages. Consuming applications should add their own provider (e.g., Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.SqlServer, Pomelo.EntityFrameworkCore.MySql).

### AppCommon.Api

HTTP, API, and web utilities for ASP.NET Core applications.

**Provides:**
- HTTP client factories
- Resilience policies (retry, circuit breaker)
- API response helpers
- Request/response middleware utilities
- Application lifecycle management (orchestrator integration)
- Configuration extensions for orchestrator-managed deployments
- Endpoint module registration for minimal APIs
- Gateway token authentication for proxy/gateway architectures
- RFC 7807 Problem Details exception handlers

**Dependencies:** AppCommon.Core, Microsoft.AspNetCore.App (FrameworkReference), Polly, Microsoft.IdentityModel.JsonWebTokens

## Dependency Strategy

Libraries are separated to isolate heavy dependencies:

1. **Core has no heavy dependencies** - Can be used anywhere
2. **AWS is isolated** - Only include if using AWS services
3. **Persistence is isolated** - Only include if using databases
4. **Web is isolated** - Only include for HTTP/API scenarios

This prevents applications from pulling in unnecessary packages.

## Usage Guide

### Installing Packages

```bash
# Core utilities (always needed)
dotnet add package AppCommon.Core

# AWS integrations (optional)
dotnet add package AppCommon.Aws

# Database patterns (optional)
dotnet add package AppCommon.Persistence

# Web/HTTP utilities (optional)
dotnet add package AppCommon.Api
```

### GitHub NuGet Source

Add to your `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/OWNER/index.json" />
  </packageSources>
</configuration>
```

## Contribution Guidelines

### Adding New Code

1. Determine which module the code belongs in
2. Add code to the appropriate `src/AppCommon.*` project
3. Add corresponding tests in `tests/AppCommon.*.Tests`
4. Ensure all tests pass before submitting

### Code Standards

- Use nullable reference types
- Add XML documentation for public APIs
- Follow existing naming conventions
- Keep methods small and focused
- Use `Guard` clauses from CommunityToolkit.Diagnostics for parameter validation

### Parameter Validation

All public methods must validate their parameters using `CommunityToolkit.Diagnostics.Guard`:

```csharp
using CommunityToolkit.Diagnostics;

public void ProcessData(string input, IList<int> items)
{
    Guard.IsNotNullOrWhiteSpace(input);
    Guard.IsNotNull(items);
    Guard.HasSizeGreaterThan(items, 0);

    // Method implementation
}
```

Common Guard methods:
- `Guard.IsNotNull(value)` - Throws if null
- `Guard.IsNotNullOrWhiteSpace(value)` - Throws if null, empty, or whitespace
- `Guard.IsNotNullOrEmpty(collection)` - Throws if null or empty collection
- `Guard.HasSizeGreaterThan(collection, size)` - Validates minimum collection size
- `Guard.IsInRange(value, min, max)` - Validates value is within range

### Testing Requirements

- All public methods must have tests
- Use xUnit for test framework
- Use NSubstitute for mocking
- Use Shouldly for assertions
- Aim for high code coverage

## Component Documentation

### Mediator

`AppCommon.Core.Mediator` provides a lightweight mediator pattern implementation for decoupling request senders from handlers, with support for pipeline behaviors.

#### Core Concepts

| Interface | Purpose |
|-----------|---------|
| `IRequest<TResponse>` | Marker interface for requests |
| `ICommand<TResponse>` | Semantic alias for commands (state-changing operations) |
| `IQuery<TResponse>` | Semantic alias for queries (read operations) |
| `IRequestHandler<TRequest, TResponse>` | Handles a specific request type |
| `IPipelineBehavior<TRequest, TResponse>` | Cross-cutting concern that wraps handler execution |
| `IMediator` | Routes requests through pipeline to handlers |

#### Request Flow

```
Request → Mediator → [Behavior1 → Behavior2 → ... → Handler] → Response
```

Behaviors wrap the handler in a delegate chain, executing in registration order (outermost to innermost).

#### Usage

**1. Define a request and handler:**

```csharp
// Request
public record CreateUserCommand(string Name, string Email) : ICommand<UserDto>;

// Handler
public class CreateUserHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(CreateUserCommand request, CancellationToken ct)
    {
        // Create user logic
        return new UserDto { Id = newId, Name = request.Name };
    }
}
```

**2. Register services:**

```csharp
services.AddMediator(typeof(Program).Assembly);
```

This auto-discovers and registers all handlers in the specified assemblies.

**3. Send requests:**

```csharp
public class UserController(IMediator mediator)
{
    public async Task<UserDto> CreateUser(CreateUserRequest request)
    {
        return await mediator.SendAsync(new CreateUserCommand(request.Name, request.Email));
    }
}
```

#### Built-in Pipeline Behaviors

**LoggingBehavior** - Logs request start, completion, and errors with timing and correlation IDs:

```csharp
services.AddPipelineBehavior(typeof(LoggingBehavior<,>));
```

Features:
- Logs at Information level for top-level requests
- Logs at Debug level for child commands in batch context (reduces noise)
- Includes duration, correlation ID, and user context

**ValidationBehavior** - Runs FluentValidation validators before the handler:

```csharp
services.AddPipelineBehavior(typeof(ValidationBehavior<,>));
```

Throws `ValidationException` if any validators fail.

#### Custom Behaviors

Implement `IPipelineBehavior<TRequest, TResponse>`:

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Check cache
        var cached = await _cache.GetAsync<TResponse>(request);
        if (cached != null) return cached;

        // Call next behavior or handler
        var response = await next();

        // Cache result
        await _cache.SetAsync(request, response);
        return response;
    }
}
```

#### Batch Execution Context

For batch operations that execute multiple commands, use `BatchExecutionContext` to reduce log verbosity:

```csharp
using (BatchExecutionContext.BeginBatch("import-users"))
{
    foreach (var user in users)
    {
        await mediator.SendAsync(new CreateUserCommand(user.Name, user.Email));
    }
}
```

Child commands within the batch context are logged at Debug level instead of Information.

---

### AppConfigurationExtensions

`AppCommon.Api.Configuration.AppConfigurationExtensions` provides a `WebApplicationBuilder` extension for Fabrica.One orchestrator configuration injection.

#### Configuration Loading Order

```
Priority (lowest → highest):
1. configuration.json  - App defaults (shipped with the app)
2. environment.json    - Environment settings (injected by orchestrator)
3. Environment variables
4. mission.json        - Deployment-specific context (injected by orchestrator)
```

Higher priority sources override lower ones for the same keys.

#### Configuration Files

| File | Purpose | Created By |
|------|---------|------------|
| `configuration.json` | App defaults, connection string templates | Developer (shipped with app) |
| `environment.json` | Environment-specific values (dev/staging/prod) | Orchestrator injects |
| `mission.json` | Deployment context (instance ID, feature flags) | Orchestrator injects |

All files are loaded from `AppContext.BaseDirectory` (where the executable resides).

#### Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddFabricaConfiguration();
```

This clears the default ASP.NET Core configuration sources (appsettings.json, etc.) and replaces them with the Fabrica.One layered approach.

#### Design Decisions

- **No appsettings.json** - Fabrica.One controls configuration externally
- **BaseDirectory** - Works correctly when orchestrator launches app from different working directories
- **reloadOnChange: false** - Files are static per deployment; no file watching overhead
- **Environment variables between file layers** - Allows both file-based and env-based overrides

---

### AppLifecycleService

`AppCommon.Core.Lifecycle.AppLifecycleService` is a hosted service that enables communication between the application and an external orchestrator (e.g., Fabrica.One) using flag files as a simple IPC mechanism.

#### Flag Files

| Flag | Created By | Meaning |
|------|------------|---------|
| `started.flag` | App | Application is fully started and ready |
| `muststop.flag` | Orchestrator | Request for graceful shutdown |
| `stopped.flag` | App | Application has fully stopped |

Each flag file contains an ISO 8601 timestamp indicating when it was created.

#### Lifecycle Flow

```
1. App starts
   └─> StartAsync() cleans up stale flags from previous runs
   └─> Registers callbacks on IHostApplicationLifetime
   └─> Starts FileSystemWatcher for muststop.flag

2. Host signals ApplicationStarted
   └─> OnStarted() creates started.flag with timestamp
   └─> Orchestrator sees this and knows app is ready

3. Orchestrator wants graceful shutdown
   └─> Creates muststop.flag
   └─> FileSystemWatcher detects it
   └─> OnMustStopCreated() calls _lifetime.StopApplication()

4. Host shuts down, signals ApplicationStopped
   └─> OnStopped() creates stopped.flag with timestamp
   └─> Orchestrator sees this and knows app has exited cleanly
```

#### Usage

Register the service in your application's DI container:

```csharp
services.AddSingleton<IHostedService, AppLifecycleService>();
```

By default, flag files are created in `AppContext.BaseDirectory`. To use a custom directory:

```csharp
services.AddSingleton<IHostedService>(sp =>
    new AppLifecycleService(
        sp.GetRequiredService<IHostApplicationLifetime>(),
        sp.GetRequiredService<ILogger<AppLifecycleService>>(),
        "/var/run/myapp"));
```

#### Why Flag Files?

Simple, cross-platform IPC that works without network ports, named pipes, or complex protocols. The orchestrator just watches/creates files. Works in containers, across process boundaries, and survives app restarts (stale flags get cleaned up on startup).

---

### EndpointModule

`AppCommon.Api.Endpoints` provides a modular endpoint registration pattern for ASP.NET Core minimal APIs, enabling feature-based endpoint organization with auto-discovery.

#### Core Concepts

| Interface/Class | Purpose |
|-----------------|---------|
| `IEndpointModule` | Interface for endpoint feature classes to implement |
| `EndpointModuleExtensions` | Extension methods for auto-discovering and registering modules |

#### Usage

**1. Define an endpoint module:**

```csharp
public class UsersEndpoint : IEndpointModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", GetAllUsers);
        app.MapGet("/users/{id}", GetUser);
        app.MapPost("/users", CreateUser);
    }

    private static async Task<IResult> GetAllUsers(IMediator mediator)
    {
        var users = await mediator.SendAsync(new GetUsersQuery());
        return Results.Ok(users);
    }

    // ... other handlers
}
```

**2. Register all endpoint modules in Program.cs:**

```csharp
var app = builder.Build();

// Maps all IEndpointModule implementations under /api prefix
app.MapEndpointModules(typeof(Program).Assembly);

// Routes become: /api/users, /api/users/{id}, etc.
```

**3. Multiple assemblies and custom prefix:**

```csharp
// Scan multiple assemblies
app.MapEndpointModules(
    [typeof(Program).Assembly, typeof(SharedEndpoints).Assembly],
    prefix: "/v1");
```

#### Benefits

- **Feature-based organization** - Colocate endpoint definitions with their handlers
- **Auto-discovery** - No manual registration required for each endpoint class
- **Consistent routing** - All routes automatically prefixed (default: `/api`)
- **Testable** - Endpoint modules can be unit tested in isolation

#### How It Works

1. `MapEndpointModules` scans the specified assemblies for concrete classes implementing `IEndpointModule`
2. Creates a route group with the specified prefix (default `/api`)
3. Instantiates each module and calls `AddRoutes`, passing the scoped route builder
4. Abstract classes and interfaces are automatically ignored

---

### GatewayTokenAuthentication

`AppCommon.Api.Identity` provides JWT-based authentication for applications running behind a gateway/proxy that handles initial authentication and forwards user claims via a header token.

#### Core Components

| Type | Purpose |
|------|---------|
| `IClaimSet` | Interface representing decoded token claims |
| `ClaimSetModel` | Default implementation of IClaimSet |
| `IGatewayTokenEncoder` | Interface for decoding JWT tokens |
| `GatewayTokenEncoder` | JWT decoder with configurable validation |
| `GatewayTokenOptions` | Configuration options for authentication |
| `GatewayIdentity` | ClaimsIdentity populated from gateway token |
| `GatewayTokenAuthenticationHandler` | ASP.NET Core authentication handler |
| `GatewayAuthenticationExtensions` | DI registration extension methods |

#### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `HeaderName` | `X-Gateway-Token` | Header containing the JWT token |
| `ValidateSignature` | `false` | Whether to validate JWT signature |
| `SigningKey` | `null` | HMAC key for signature validation |
| `ValidateExpiration` | `true` | Whether to check token expiration |
| `ClockSkew` | 5 minutes | Tolerance for expiration checks |
| `DevelopmentToken` | `null` | Fallback token for local development |

#### Usage

**Basic setup (behind trusted proxy):**

```csharp
// Program.cs
builder.Services.AddGatewayTokenAuthentication();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

**With signature validation:**

```csharp
builder.Services.AddGatewayTokenAuthentication(options =>
{
    options.ValidateSignature = true;
    options.SigningKey = builder.Configuration["Auth:SigningKey"];
});
```

**Development fallback token:**

```csharp
builder.Services.AddGatewayTokenAuthentication(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DevelopmentToken = "eyJ..."; // Pre-generated dev token
    }
});
```

**Adding to existing authentication:**

```csharp
builder.Services
    .AddAuthentication()
    .AddGatewayToken()
    .AddJwtBearer(); // Other schemes
```

#### How It Works

1. Request arrives with `X-Gateway-Token` header (or configured header name)
2. Handler extracts and decodes the JWT token
3. If `ValidateSignature` is true, signature is verified
4. If `ValidateExpiration` is true, expiration is checked (with clock skew)
5. Claims are extracted and `ICurrentUserService` is populated
6. `ClaimsPrincipal` is created with standard claim types (NameIdentifier, Name, Email, Role)

#### Token Format

The handler expects standard JWT claims:

```json
{
  "sub": "user-123",
  "name": "John Doe",
  "email": "john@example.com",
  "roles": ["admin", "user"],
  "exp": 1234567890
}
```

#### When to Use

- Applications behind an API gateway that handles OAuth/OIDC
- Microservices receiving pre-validated tokens from a gateway
- Local development that needs to simulate gateway behavior

---

### Exception Handlers

`AppCommon.Api.Middleware` provides RFC 7807 Problem Details-compliant exception handlers for ASP.NET Core applications.

#### Components

| Handler | Purpose |
|---------|---------|
| `ValidationExceptionHandler` | Handles FluentValidation exceptions with grouped errors |
| `GlobalExceptionHandler` | Catches all unhandled exceptions as a fallback |

#### Exception to Status Code Mapping

| Exception Type | Status Code | Description |
|----------------|-------------|-------------|
| `ValidationException` | 400 | Validation errors with field-level details |
| `KeyNotFoundException` | 404 | Resource not found |
| `FileNotFoundException` | 404 | Resource not found |
| `UnauthorizedAccessException` | 401 | Authentication required |
| `InvalidOperationException` | 400 | Bad request |
| `ArgumentException` | 400 | Bad request |
| `OperationCanceledException` | 499 | Client closed request |
| `TimeoutException` | 504 | Gateway timeout |
| Other exceptions | 500 | Internal server error |

#### Usage

```csharp
// Program.cs
builder.Services.AddProblemDetails();

// Register handlers in order (specific first, global last)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
```

#### Response Format

All exceptions return RFC 7807 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors property for details.",
  "traceId": "00-abc123...",
  "errors": {
    "Name": ["Name is required", "Name must be at least 3 characters"],
    "Email": ["Email is invalid"]
  }
}
```

#### Development Mode

In development environment, `GlobalExceptionHandler` includes additional debugging information:
- `exceptionType`: Full type name of the exception
- `stackTrace`: Stack trace (excluded for 404s and cancellations)
- `innerException`: Inner exception details if present

In production, these details are hidden to prevent information leakage.

---

### AuditSaveChangesInterceptor

`AppCommon.Persistence.Interceptors.AuditSaveChangesInterceptor` is an EF Core interceptor that automatically creates audit trail entries when entities are saved.

#### Core Types (AppCommon.Core.Persistence)

| Type | Purpose |
|------|---------|
| `IEntity` | Base interface with `GetUid()` for all domain entities |
| `IRootEntity` | Marker interface for aggregate root entities |
| `IAggregateChild` | Interface for entities belonging to an aggregate with `GetRoot()` |
| `BaseEntity<T>` | Abstract base class with identity-based equality |
| `AuditAttribute` | Marks entities for audit logging |
| `AuditJournal` | Audit log entry entity |
| `AuditJournalType` | Enum: Created, Updated, Deleted, Detail, UnmodifiedRoot |

#### Audit Journal Hierarchy

The flat `AuditJournal` table encodes a 3-level logical hierarchy:

```
When + Who (CorrelationUid + OccurredAt + Subject + UserName)
└── What Entity (EntityType + EntityUid + EntityDescription + TypeCode)
    └── What Property (PropertyName + PreviousValue + CurrentValue)
```

| Level | Fields | Description |
|-------|--------|-------------|
| Transaction | `CorrelationUid`, `OccurredAt`, `Subject`, `UserName` | A single user action at a point in time |
| Entity | `EntityType`, `EntityUid`, `EntityDescription`, `TypeCode` | What entities were affected |
| Property | `PropertyName`, `PreviousValue`, `CurrentValue` | What properties changed (Detail entries) |

#### How It Works

1. Intercepts `SaveChanges`/`SaveChangesAsync` before changes are persisted
2. Scans `ChangeTracker` for entities marked with `[Audit]` attribute
3. Creates audit entries based on entity state:
   - `Added` → `AuditJournalType.Created` + Detail entries
   - `Modified` → `AuditJournalType.Updated` + Detail entries for changed properties
   - `Deleted` → `AuditJournalType.Deleted`
4. For `IAggregateChild` modifications where the root wasn't directly changed, creates `UnmodifiedRoot` entry to link changes to the aggregate root
5. All entries share the same `CorrelationUid` (from `Activity.Current?.TraceId` or new ULID)
6. User context captured from `ICurrentUserService`

#### Usage

**1. Mark entities for auditing:**

```csharp
[Audit]
public class Customer : BaseEntity<Customer>, IRootEntity
{
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Name { get; set; }

    public override string GetUid() => Uid;
}

[Audit]
public class Order : BaseEntity<Order>, IAggregateChild
{
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public Customer Customer { get; set; }

    public override string GetUid() => Uid;
    public IRootEntity? GetRoot() => Customer;
}
```

**2. Configure the interceptor:**

```csharp
services.AddScoped<ICurrentUserService, CurrentUserService>();
services.AddScoped<AuditSaveChangesInterceptor>();

services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseMySql(connectionString, serverVersion)
           .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});
```

**3. Include AuditJournal in your DbContext:**

```csharp
public class AppDbContext : DbContext
{
    public DbSet<AuditJournal> AuditJournals => Set<AuditJournal>();
}
```

#### Audit Attribute Options

| Option | Default | Description |
|--------|---------|-------------|
| `Write` | `true` | Whether to create audit entries for this entity |
| `Detailed` | `true` | Whether to track property-level changes |
| `EntityName` | `null` | Custom name for audit log (defaults to full type name) |

#### Query Patterns

```csharp
// What changed in request X?
var changes = await db.AuditJournals
    .Where(a => a.CorrelationUid == correlationUid)
    .ToListAsync();

// History of entity Y?
var history = await db.AuditJournals
    .Where(a => a.EntityUid == entityUid)
    .OrderByDescending(a => a.OccurredAt)
    .ToListAsync();

// All changes by user Z?
var userChanges = await db.AuditJournals
    .Where(a => a.Subject == userId)
    .ToListAsync();
```

---

### AuditJournalExtensions

`AppCommon.Core.Persistence.AuditJournalExtensions` provides extension methods to transform flat `AuditJournal` entries into a hierarchical structure for display.

#### Hierarchy DTOs

| Type | Level | Properties |
|------|-------|------------|
| `AuditTransactionGroup` | Top (When + Who) | `CorrelationUid`, `Subject`, `UserName`, `OccurredAt`, `Entities` |
| `AuditEntityGroup` | Middle (What) | `TypeCode`, `EntityType`, `EntityUid`, `EntityDescription`, `Properties` |
| `AuditPropertyChange` | Bottom (Detail) | `PropertyName`, `PreviousValue`, `CurrentValue` |

#### Extension Methods

**`ToHierarchy()`** - Transforms flat entries into hierarchical structure:

```csharp
var flatEntries = await db.AuditJournals
    .Where(a => a.EntityUid == clientUid)
    .ToListAsync();

List<AuditTransactionGroup> hierarchy = flatEntries.ToHierarchy();
// Returns transactions ordered by OccurredAt descending
```

**`ToHierarchyForEntity(entityUid)`** - Filters to transactions containing a specific entity:

```csharp
var allEntries = await db.AuditJournals.ToListAsync();

// Find all transactions where this entity was involved
List<AuditTransactionGroup> entityHistory = allEntries.ToHierarchyForEntity(clientUid);
```

#### How It Works

1. Groups entries by `CorrelationUid` (one group per transaction)
2. For each transaction, extracts entity-level entries (Created, Updated, Deleted)
3. For each entity, attaches matching Detail entries as property changes
4. Orders transactions by `OccurredAt` descending (newest first)

#### UnmodifiedRoot Handling

`UnmodifiedRoot` entries are used for **querying** but **excluded from output**:

- **Querying**: `ToHierarchyForEntity()` finds transactions where an entity appears as `UnmodifiedRoot` (e.g., when a child entity changed but the root didn't)
- **Output**: `UnmodifiedRoot` entries are filtered out of the hierarchy since they don't represent actual changes

This allows you to find "all changes affecting Customer X" including changes to child Orders, without displaying a redundant "Customer unchanged" entry.

#### Example Output

```csharp
// Single transaction with one updated entity and two property changes
[
  {
    CorrelationUid: "corr-123",
    Subject: "user-456",
    UserName: "John Doe",
    OccurredAt: "2025-01-29T10:30:00Z",
    Entities: [
      {
        TypeCode: "Updated",
        EntityType: "Customer",
        EntityUid: "cust-789",
        EntityDescription: "Customer: Acme Corp",
        Properties: [
          { PropertyName: "Name", PreviousValue: "Acme", CurrentValue: "Acme Corp" },
          { PropertyName: "Email", PreviousValue: "old@acme.com", CurrentValue: "new@acme.com" }
        ]
      }
    ]
  }
]
```

---

## Build & Publish

### Local Build

```bash
# Build solution
dotnet build app-common.slnx

# Run tests
dotnet test app-common.slnx

# Or use Cake
cd build && dotnet run
```

### Creating Packages

```bash
cd build && dotnet run -- --target=Pack
```

This will:
1. Clean the solution
2. Restore packages
3. Build in Release mode
4. Run all tests
5. Increment version
6. Create NuGet packages in `artifacts/`

### Publishing Packages

```bash
cd build && dotnet run -- --target=Push --nuget-source="https://nuget.pkg.github.com/OWNER/index.json" --nuget-api-key="YOUR_TOKEN"
```

## Version Management

Version is tracked in `version.json`:

```json
{
  "major": 1,
  "minor": 0,
  "build": 0
}
```

- **Major**: Breaking changes
- **Minor**: New features (backwards compatible)
- **Build**: Auto-incremented on pack

## Project Structure

```
app-common/
├── PRD.md                        # This document
├── src/
│   ├── AppCommon.Core/           # Base utilities
│   ├── AppCommon.Aws/            # AWS integrations
│   ├── AppCommon.Persistence/    # Database patterns
│   └── AppCommon.Api/            # Web utilities
├── build/
│   └── Build.csproj              # Cake Frosting build
├── tests/
│   ├── AppCommon.Core.Tests/
│   ├── AppCommon.Aws.Tests/
│   ├── AppCommon.Persistence.Tests/
│   └── AppCommon.Api.Tests/
├── Directory.Build.props         # Common build settings
├── Directory.Packages.props      # Central Package Management
├── version.json                  # Version tracking
└── app-common.slnx               # Solution file
```
