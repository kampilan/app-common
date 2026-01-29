# AppCommon.Api

HTTP, API, and web utilities for ASP.NET Core applications.

## Features

- **Endpoint Modules** - Auto-discovery for minimal API endpoints
- **Gateway Authentication** - JWT token auth for proxy/gateway architectures
- **Exception Handlers** - RFC 7807 Problem Details responses
- **Configuration Extensions** - Fabrica.One orchestrator integration

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

// Gateway token authentication
builder.Services.AddGatewayTokenAuthentication();

// Exception handlers
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();

// Auto-discover and register endpoint modules
app.MapEndpointModules(typeof(Program).Assembly);

app.Run();
```

## Endpoint Modules

```csharp
public class UsersEndpoint : IEndpointModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", GetAllUsers);
        app.MapPost("/users", CreateUser);
    }

    private static async Task<IResult> GetAllUsers(IMediator mediator)
        => Results.Ok(await mediator.SendAsync(new GetUsersQuery()));
}
```

## Key Types

| Type | Purpose |
|------|---------|
| `IEndpointModule` | Interface for endpoint feature classes |
| `MapEndpointModules()` | Auto-discovers and registers endpoints |
| `AddGatewayTokenAuthentication()` | JWT auth from gateway header |
| `ValidationExceptionHandler` | FluentValidation → Problem Details |
| `GlobalExceptionHandler` | Catch-all exception handler |
| `AddFabricaConfiguration()` | Orchestrator config loading |

## Gateway Authentication Options

```csharp
builder.Services.AddGatewayTokenAuthentication(options =>
{
    options.HeaderName = "X-Gateway-Token";
    options.ValidateSignature = true;
    options.SigningKey = "your-key";
    options.ValidateExpiration = true;
});
```

## Documentation

See [full documentation](https://github.com/kampilan/app-common/blob/main/PRD.md) for detailed usage.
