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

**Dependencies:** CommunityToolkit.Diagnostics, FluentValidation, Microsoft.Extensions.* abstractions

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

**Dependencies:** AppCommon.Core, Entity Framework Core (provider-agnostic)

Note: This library does not include any database provider packages. Consuming applications should add their own provider (e.g., Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.SqlServer).

### AppCommon.Api

HTTP, API, and web utilities for ASP.NET Core applications.

**Provides:**
- HTTP client factories
- Resilience policies (retry, circuit breaker)
- API response helpers
- Request/response middleware utilities
- Application lifecycle management (orchestrator integration)
- Configuration extensions for orchestrator-managed deployments

**Dependencies:** AppCommon.Core, Microsoft.AspNetCore.App (FrameworkReference), Polly

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

`AppCommon.Api.Lifecycle.AppLifecycleService` is a hosted service that enables communication between the application and an external orchestrator (e.g., Fabrica.One) using flag files as a simple IPC mechanism.

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
