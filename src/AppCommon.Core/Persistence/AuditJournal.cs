namespace AppCommon.Core.Persistence;

/// <summary>
/// Represents an audit log entry for tracking changes to domain entities.
/// </summary>
public class AuditJournal : BaseEntity<AuditJournal>
{
 
    public long Id { get; set; }

    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    
    /// <summary>
    /// Correlation UID to group changes that occurred in the same request/operation.
    /// Uses TraceId for HTTP requests, ULID for background operations.
    /// </summary>
    public string CorrelationUid { get; set; } = string.Empty;

    /// <summary>
    /// The subject (user identifier) who made the change.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the user who made the change.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// When the change occurred (UTC).
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// The type of audit entry: Created, Updated, Deleted, or Detail.
    /// </summary>
    public string TypeCode { get; set; } = string.Empty;

    /// <summary>
    /// The full type name of the entity (e.g., "Crm.Domain.Entities.Client").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The Uid of the audited entity.
    /// </summary>
    public string EntityUid { get; set; } = string.Empty;

    /// <summary>
    /// A description of the entity (typically from ToString()).
    /// </summary>
    public string EntityDescription { get; set; } = string.Empty;

    /// <summary>
    /// For Detail entries, the name of the property that changed.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// For Detail entries, the original value (truncated to 500 chars).
    /// </summary>
    public string? PreviousValue { get; set; }

    /// <summary>
    /// For Detail entries, the new value (truncated to 500 chars).
    /// </summary>
    public string? CurrentValue { get; set; }

    public override string GetUid()
    {
        return Uid;
    }
}
