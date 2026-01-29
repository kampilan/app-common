# AppCommon.Persistence

Database and data access patterns for .NET applications using Entity Framework Core.

## Features

- **Automatic Audit Logging** - EF Core interceptor tracks entity changes
- **Aggregate Pattern Support** - `IRootEntity` and `IAggregateChild` for audit correlation
- **Provider Agnostic** - Works with any EF Core database provider

## Quick Start

```csharp
// Mark entities for auditing
[Audit]
public class Customer : BaseEntity<Customer>, IRootEntity
{
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Name { get; set; } = string.Empty;

    public override string GetUid() => Uid;
}

[Audit]
public class Order : BaseEntity<Order>, IAggregateChild
{
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public Customer Customer { get; set; } = null!;

    public override string GetUid() => Uid;
    public IRootEntity? GetRoot() => Customer;
}

// Configure DbContext with audit interceptor
services.AddScoped<AuditSaveChangesInterceptor>();
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseMySql(connectionString, serverVersion)
           .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});
```

## Key Types

| Type | Purpose |
|------|---------|
| `AuditSaveChangesInterceptor` | Auto-creates audit entries on SaveChanges |
| `[Audit]` | Marks entity for audit logging |
| `AuditJournal` | Audit log entry (in AppCommon.Core) |
| `IRootEntity` | Aggregate root marker |
| `IAggregateChild` | Child entity with `GetRoot()` |

## Audit Attribute Options

```csharp
[Audit]                          // Full auditing
[Audit(Detailed = false)]        // No property-level tracking
[Audit(Write = false)]           // Read-only audit (queries only)
[Audit(EntityName = "Client")]   // Custom name in audit log
```

## Documentation

See [full documentation](https://github.com/kampilan/app-common/blob/main/PRD.md) for detailed usage.
