# AppCommon AI Assistant Guide

This file provides context for AI assistants (Claude, etc.) working with projects that use AppCommon packages.

## Package Overview

| Package | Purpose |
|---------|---------|
| `AppCommon.Core` | Mediator, lifecycle, audit infrastructure, validation, `IRequestContext` interface |
| `AppCommon.Aws` | AWS client DI registration, EC2 metadata |
| `AppCommon.Persistence` | EF Core audit interceptor (uses `IRequestContext`) |
| `AppCommon.Api` | Minimal API endpoints, gateway auth, exception handlers, `IRequestContext` implementation |

## Component Relationships

Understanding how components connect across packages is critical for debugging and implementation.

### Request Context - The Central Bridge

`IRequestContext` is the unified way to access user identity and correlation information. It reads directly from `HttpContext.User` - no manual population required.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              REQUEST FLOW                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  HTTP Request                                                                │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────┐                                    │
│  │ Any Auth Handler (JWT, Cookie, etc) │                                    │
│  │ - Populates HttpContext.User        │                                    │
│  └─────────────────────────────────────┘                                    │
│                    │                                                         │
│                    ▼                                                         │
│  ┌─────────────────────────────────────┐                                    │
│  │ IRequestContext (reads from User)   │  (AppCommon.Api implementation)    │
│  │ - Subject (user ID from sub claim)  │                                    │
│  │ - UserName, UserEmail, Roles        │                                    │
│  │ - CorrelationUid (TraceId or Ulid)  │                                    │
│  │ - IsAuthenticated                   │                                    │
│  └─────────────────────────────────────┘                                    │
│                    │                                                         │
│       ┌────────────┴────────────┐                                           │
│       ▼                         ▼                                            │
│  ┌──────────────────┐    ┌─────────────────────────────────┐                │
│  │ LoggingBehavior  │    │ AuditSaveChangesInterceptor     │                │
│  │ - Logs Subject   │    │ - Records Subject + UserName    │                │
│  │ - CorrelationUid │    │ - Uses CorrelationUid           │                │
│  │  (AppCommon.Core)│    │         (AppCommon.Persistence) │                │
│  └──────────────────┘    └─────────────────────────────────┘                │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Key insight:** `IRequestContext` reads from `HttpContext.User` automatically. Any authentication handler that populates `HttpContext.User` (JWT, cookies, gateway tokens, etc.) will automatically be reflected in `IRequestContext`.

### Mediator Request Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MEDIATOR PIPELINE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  IMediator.SendAsync(command)                                               │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────┐                                        │
│  │ LoggingBehavior (if registered) │                                        │
│  │ - Logs request start + Subject  │                                        │
│  │ - Uses CorrelationUid           │                                        │
│  └───────────────┬─────────────────┘                                        │
│                  ▼                                                           │
│  ┌─────────────────────────────────┐                                        │
│  │ ValidationBehavior (if reg.)    │                                        │
│  │ - Runs FluentValidation         │                                        │
│  │ - Throws ValidationException    │                                        │
│  └───────────────┬─────────────────┘                                        │
│                  ▼                                                           │
│  ┌─────────────────────────────────┐                                        │
│  │ IRequestHandler<T>              │                                        │
│  │ - Your business logic           │                                        │
│  │ - May call DbContext.SaveChanges│───┐                                    │
│  └───────────────┬─────────────────┘   │                                    │
│                  │                      ▼                                    │
│                  │    ┌─────────────────────────────────┐                   │
│                  │    │ AuditSaveChangesInterceptor     │                   │
│                  │    │ - Creates AuditJournal entries  │                   │
│                  │    │ - Uses IRequestContext          │                   │
│                  │    └─────────────────────────────────┘                   │
│                  ▼                                                           │
│  ┌─────────────────────────────────┐                                        │
│  │ LoggingBehavior                 │                                        │
│  │ - Logs completion + duration    │                                        │
│  └─────────────────────────────────┘                                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Audit Entity Relationships

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AUDIT ENTITY MODEL                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  [Audit]                                                                     │
│  Customer : BaseEntity<Customer>, IRootEntity                               │
│       │                                                                      │
│       │ GetRoot() returns this                                              │
│       │                                                                      │
│       └──────┬───────────────┐                                              │
│              │               │                                               │
│              ▼               ▼                                               │
│  [Audit]                 [Audit]                                            │
│  Order : IAggregateChild  Address : IAggregateChild                         │
│  GetRoot() → Customer     GetRoot() → Customer                              │
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  When Order is modified but Customer is not:                                │
│  - AuditJournal entry created for Order (TypeCode: Updated)                 │
│  - AuditJournal entry created for Customer (TypeCode: UnmodifiedRoot)       │
│  - Both share same CorrelationUid                                           │
│                                                                              │
│  This allows querying "all changes affecting Customer X" to include         │
│  child entity changes even when the root wasn't directly modified.          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Common Patterns

### Mediator (CQRS)

```csharp
// Commands change state, Queries read state
public record CreateUserCommand(string Name) : ICommand<UserDto>;
public record GetUserQuery(string Id) : IQuery<UserDto>;

// Handlers
public class CreateUserHandler : ICommandHandler<CreateUserCommand, UserDto> { ... }

// Send via IMediator
var result = await mediator.SendAsync(new CreateUserCommand("John"));
```

### Request Context

Access user identity and correlation info anywhere via DI:

```csharp
public class MyHandler(IRequestContext context)
{
    public void Handle()
    {
        // User info (from HttpContext.User claims)
        var userId = context.Subject;          // From "sub" claim
        var name = context.UserName;           // From "name" claim
        var email = context.UserEmail;         // From "email" claim
        var roles = context.Roles;             // From role claims
        var isAuth = context.IsAuthenticated;

        // Correlation (for distributed tracing)
        var correlationId = context.CorrelationUid;  // TraceId or Ulid
    }
}
```

### Audit Logging

Entities marked with `[Audit]` are automatically logged when saved via EF Core:

```csharp
[Audit]
public class Customer : BaseEntity<Customer>, IRootEntity
{
    public override string GetUid() => Uid;
}

// Child entities link to their root for correlation
[Audit]
public class Order : BaseEntity<Order>, IAggregateChild
{
    public Customer Customer { get; set; }
    public IRootEntity? GetRoot() => Customer;
}
```

### Startup Services

Services implementing `IRequiresStart` are auto-initialized at startup:

```csharp
services.AddStartupServices(); // Registers StartupHostedService
services.AddAws();             // InstanceMetaService implements IRequiresStart
```

### Endpoint Modules

Group related endpoints in feature classes:

```csharp
public class OrdersEndpoint : IEndpointModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", GetOrders);
        app.MapPost("/orders", CreateOrder);
    }
}

// Auto-discover all modules (no prefix by default)
app.MapEndpointModules(typeof(Program).Assembly);

// Or with a prefix
app.MapEndpointModules(typeof(Program).Assembly, prefix: "/api");
```

### Gateway Authentication

For apps behind a gateway that forwards JWT tokens:

```csharp
// 1. Register request context and authentication
services.AddRequestContext();  // Provides IRequestContext
services.AddGatewayTokenAuthentication(options =>
{
    options.HeaderName = "X-Gateway-Token";  // Default header name
});

// 2. Use authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// 3. IRequestContext automatically reflects the authenticated user
public class MyHandler(IRequestContext context)
{
    public void Handle() => Console.WriteLine(context.Subject);
}
```

### Exception Handling

RFC 7807 Problem Details responses:

```csharp
services.AddExceptionHandler<ValidationExceptionHandler>(); // 400 for FluentValidation
services.AddExceptionHandler<GlobalExceptionHandler>();     // Catch-all
app.UseExceptionHandler();
```

## DI Registration Cheat Sheet

```csharp
// Core
services.AddMediator(typeof(Program).Assembly);
services.AddPipelineBehavior(typeof(LoggingBehavior<,>));
services.AddPipelineBehavior(typeof(ValidationBehavior<,>));
services.AddStartupServices();

// AWS
services.AddAws(options => { options.DefaultRegion = "us-west-2"; });
services.AddS3Client();
services.AddSqsClient();
services.AddDynamoDbClient("prefix_");

// API - Request Context (required for LoggingBehavior and AuditInterceptor)
services.AddRequestContext();

// Persistence (uses IRequestContext for audit entries)
services.AddScoped<AuditSaveChangesInterceptor>();
services.AddDbContext<AppDbContext>((sp, opt) =>
    opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

// API - Authentication (optional, populates HttpContext.User)
services.AddGatewayTokenAuthentication();
services.AddExceptionHandler<ValidationExceptionHandler>();
services.AddExceptionHandler<GlobalExceptionHandler>();
app.MapEndpointModules(typeof(Program).Assembly);
```

## Audit Journal Hierarchy

Flat `AuditJournal` records encode a 3-level hierarchy:

```
Transaction (CorrelationUid + OccurredAt + Subject + UserName)
└── Entity (EntityType + EntityUid + TypeCode)
    └── Property (PropertyName + PreviousValue + CurrentValue)
```

Use `entries.ToHierarchy()` to build the hierarchy for display.

## Key Interfaces

| Interface | Purpose | Package |
|-----------|---------|---------|
| `IMediator` | Send commands/queries through pipeline | Core |
| `IRequestContext` | Access user identity and correlation (reads from HttpContext.User) | Core (interface), Api (implementation) |
| `IInstanceMetadata` | EC2 metadata (or defaults) | Aws |
| `IRequiresStart` | Service needing initialization at startup | Core |
| `IEndpointModule` | Minimal API endpoint group | Api |
| `IEntity` | Base for auditable entities | Core |
| `IRootEntity` | Aggregate root marker | Core |
| `IAggregateChild` | Entity belonging to aggregate, links to root | Core |
| `IGatewayTokenEncoder` | Decodes JWT tokens for gateway auth | Api |

## Troubleshooting

### User info missing in audit logs or LoggingBehavior
1. Ensure `services.AddRequestContext()` is called
2. Ensure `app.UseAuthentication()` is called before endpoints
3. Check that your auth handler populates `HttpContext.User`
4. `IRequestContext` reads directly from `HttpContext.User` - no manual setup needed

### Subject is null but user should be authenticated
1. `IRequestContext.Subject` reads from `ClaimTypes.NameIdentifier` (the "sub" claim)
2. Verify your JWT/auth token includes the "sub" claim
3. Check `HttpContext.User.Identity.IsAuthenticated` to confirm auth succeeded

### Endpoints not found (404)
1. Verify `MapEndpointModules()` is called with the correct assembly
2. Check if you're expecting a prefix - default is no prefix
3. Use `app.MapEndpointModules(assembly, prefix: "/api")` if you need `/api` prefix

### Audit entries not created
1. Entity must have `[Audit]` attribute
2. Entity must implement `IEntity` (use `BaseEntity<T>`)
3. `AuditSaveChangesInterceptor` must be registered and added to DbContext
4. `services.AddRequestContext()` must be called

### CorrelationUid in logs doesn't match audit entries
1. Both `LoggingBehavior` and `AuditSaveChangesInterceptor` use `IRequestContext.CorrelationUid`
2. This value comes from `Activity.Current.TraceId` (if available) or a generated ULID
3. All should be consistent within a single HTTP request

## Source Code

Repository: https://github.com/kampilan/app-common
Full documentation: https://github.com/kampilan/app-common/blob/main/PRD.md
