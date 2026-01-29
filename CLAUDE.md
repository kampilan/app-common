# AppCommon AI Assistant Guide

This file provides context for AI assistants (Claude, etc.) working with projects that use AppCommon packages.

## Package Overview

| Package | Purpose |
|---------|---------|
| `AppCommon.Core` | Mediator, lifecycle, audit infrastructure, validation |
| `AppCommon.Aws` | AWS client DI registration, EC2 metadata |
| `AppCommon.Persistence` | EF Core audit interceptor |
| `AppCommon.Api` | Minimal API endpoints, gateway auth, exception handlers |

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

// Auto-discover all modules
app.MapEndpointModules(typeof(Program).Assembly);
```

### Gateway Authentication

For apps behind a gateway that forwards JWT tokens:

```csharp
services.AddGatewayTokenAuthentication(options =>
{
    options.HeaderName = "X-Gateway-Token";
});

// Access current user via ICurrentUserService
public class MyHandler(ICurrentUserService user)
{
    public void Handle() => Console.WriteLine(user.UserId);
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

// Persistence
services.AddScoped<AuditSaveChangesInterceptor>();
services.AddDbContext<AppDbContext>((sp, opt) =>
    opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

// API
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

| Interface | Purpose |
|-----------|---------|
| `IMediator` | Send commands/queries |
| `ICurrentUserService` | Access authenticated user |
| `IInstanceMetadata` | EC2 metadata (or defaults) |
| `IRequiresStart` | Service needing initialization |
| `IEndpointModule` | Minimal API endpoint group |
| `IEntity` | Base for auditable entities |
| `IRootEntity` | Aggregate root marker |
| `IAggregateChild` | Entity belonging to aggregate |

## Source Code

Repository: https://github.com/kampilan/app-common
Full documentation: https://github.com/kampilan/app-common/blob/main/PRD.md
