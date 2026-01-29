namespace AppCommon.Core.Persistence;

/// <summary>
/// Top level of the audit hierarchy: When + Who.
/// Groups all changes from a single transaction/request.
/// </summary>
public record AuditTransactionGroup(
    string CorrelationUid,
    string Subject,
    string UserName,
    DateTime OccurredAt,
    List<AuditEntityGroup> Entities);

/// <summary>
/// Second level of the audit hierarchy: What Entity.
/// Represents a single entity that was created, updated, deleted, or marked as unmodified root.
/// </summary>
public record AuditEntityGroup(
    string TypeCode,
    string EntityType,
    string EntityUid,
    string EntityDescription,
    List<AuditPropertyChange> Properties);

/// <summary>
/// Third level of the audit hierarchy: What Property.
/// Represents a single property change with previous and current values.
/// </summary>
public record AuditPropertyChange(
    string PropertyName,
    string? PreviousValue,
    string? CurrentValue);
