# AppCommon.Core

Base utilities, extensions, and common patterns for .NET applications.

## Features

- **Mediator Pattern** - Request/response with pipeline behaviors
- **Lifecycle Management** - `IRequiresStart` for service initialization, `AppLifecycleService` for orchestrator integration
- **Audit Infrastructure** - Base entity types, audit journal, and hierarchy builders
- **Validation** - FluentValidation integration with pipeline behavior

## Quick Start

```csharp
// Mediator setup
services.AddMediator(typeof(Program).Assembly);
services.AddPipelineBehavior(typeof(ValidationBehavior<,>));

// Startup services (auto-initializes IRequiresStart implementations)
services.AddStartupServices();

// Send a command
var result = await mediator.SendAsync(new CreateUserCommand("John", "john@example.com"));
```

## Key Types

| Type | Purpose |
|------|---------|
| `IMediator` | Routes requests to handlers |
| `ICommand<T>` / `IQuery<T>` | Semantic request markers |
| `IPipelineBehavior<,>` | Cross-cutting concerns |
| `IRequiresStart` | Services needing initialization |
| `StartupHostedService` | Auto-starts `IRequiresStart` services |
| `AppLifecycleService` | Flag-file based orchestrator integration |
| `IEntity` / `IRootEntity` | Audit infrastructure interfaces |
| `AuditJournal` | Audit log entry entity |

## Documentation

See [full documentation](https://github.com/kampilan/app-common/blob/main/PRD.md) for detailed usage.
