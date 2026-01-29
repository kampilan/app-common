namespace AppCommon.Core.Persistence;

/// <summary>
/// Marks an entity class for audit logging. When applied, changes to the entity
/// will be recorded in the AuditJournal table.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AuditAttribute : Attribute
{
    /// <summary>
    /// Whether to audit create/update/delete operations. Default: true.
    /// </summary>
    public bool Write { get; set; } = true;

    /// <summary>
    /// Whether to track property-level changes. Default: true.
    /// When enabled, individual property changes are recorded with original and current values.
    /// </summary>
    public bool Detailed { get; set; } = true;

    /// <summary>
    /// Custom entity name for the audit log. If not set, the full type name is used.
    /// </summary>
    public string? EntityName { get; set; }
}
