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

HTTP, API, and web utilities.

**Provides:**
- HTTP client factories
- Resilience policies (retry, circuit breaker)
- API response helpers
- Request/response middleware utilities

**Dependencies:** AppCommon.Core, Microsoft.Extensions.Http, Polly

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
