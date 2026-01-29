# AppCommon

A modular class library ecosystem for code reuse across Claude-built applications. Published to GitHub NuGet repository.

## Modules

| Package | Purpose |
|---------|---------|
| **AppCommon.Core** | Base utilities, extensions, mediator pattern, audit infrastructure |
| **AppCommon.Aws** | AWS SDK integrations (S3, Secrets Manager, SSM, DynamoDB, SQS) |
| **AppCommon.Persistence** | EF Core utilities, audit interceptor (provider-agnostic) |
| **AppCommon.Api** | Minimal API endpoints, gateway auth, exception handlers, lifecycle |

## Installation

```bash
dotnet add package AppCommon.Core
dotnet add package AppCommon.Aws          # Optional: AWS integrations
dotnet add package AppCommon.Persistence  # Optional: Database patterns
dotnet add package AppCommon.Api          # Optional: Web/API utilities
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

## Quick Start

### Mediator Pattern

```csharp
// Define a command
public record CreateUserCommand(string Name, string Email) : ICommand<UserDto>;

// Implement handler
public class CreateUserHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(CreateUserCommand request, CancellationToken ct)
    {
        // Create user logic
        return new UserDto { Name = request.Name };
    }
}

// Register and use
services.AddMediator(typeof(Program).Assembly);

var user = await mediator.SendAsync(new CreateUserCommand("John", "john@example.com"));
```

### Automatic Audit Logging

```csharp
// Mark entities for auditing
[Audit]
public class Customer : BaseEntity<Customer>, IRootEntity
{
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Name { get; set; }
    public override string GetUid() => Uid;
}

// Configure interceptor
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseMySql(connectionString, serverVersion)
           .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});
```

### Minimal API Endpoints

```csharp
// Define endpoint module
public class UsersEndpoint : IEndpointModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", GetAllUsers);
        app.MapPost("/users", CreateUser);
    }
}

// Auto-discover and register
app.MapEndpointModules(typeof(Program).Assembly);
```

### Gateway Token Authentication

```csharp
builder.Services.AddGatewayTokenAuthentication(options =>
{
    options.HeaderName = "X-Gateway-Token";
    options.ValidateExpiration = true;
});
```

## Building

```bash
# Build
dotnet build app-common.slnx

# Test
dotnet test app-common.slnx

# Pack (using Cake)
cd build && dotnet run -- --target=Pack
```

## Documentation

See [PRD.md](PRD.md) for detailed documentation including:

- Module descriptions and dependencies
- Component documentation (Mediator, Audit System, Endpoints, etc.)
- Code standards and contribution guidelines
- Build and publish instructions

## License

MIT
