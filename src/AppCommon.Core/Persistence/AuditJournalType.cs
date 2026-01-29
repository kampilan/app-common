namespace AppCommon.Core.Persistence;

/// <summary>
/// Defines the type of audit journal entry.
/// </summary>
public enum AuditJournalType
{
    /// <summary>
    /// Entity was created.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Entity was updated.
    /// </summary>
    Updated = 2,

    /// <summary>
    /// Entity was deleted.
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// Property-level change detail.
    /// </summary>
    Detail = 10,

    /// <summary>
    /// Root entity that owns modified aggregate children but wasn't itself modified.
    /// </summary>
    UnmodifiedRoot = 20
}
