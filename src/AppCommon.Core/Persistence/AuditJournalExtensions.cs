namespace AppCommon.Core.Persistence;

/// <summary>
/// Extension methods for working with audit journal entries.
/// </summary>
public static class AuditJournalExtensions
{
    /// <summary>
    /// Transforms a flat list of audit journal entries into a hierarchical structure.
    ///
    /// Hierarchy:
    /// - Transaction (When + Who): CorrelationUid, OccurredAt, Subject, UserName
    ///   - Entity (What): EntityType, EntityUid, EntityDescription, TypeCode
    ///     - Property (Detail): PropertyName, PreviousValue, CurrentValue
    /// </summary>
    /// <param name="entries">Flat list of audit journal entries.</param>
    /// <returns>Hierarchical list of transaction groups, ordered by OccurredAt descending.</returns>
    public static List<AuditTransactionGroup> ToHierarchy(this IEnumerable<AuditJournal> entries)
    {
        var entryList = entries.ToList();

        return entryList
            .GroupBy(aj => aj.CorrelationUid)
            .Select(corrGroup =>
            {
                // Groups from GroupBy always have at least one element
                var first = corrGroup.First();

                var entityGroups = corrGroup
                    .Where(aj => aj.TypeCode != AuditJournalType.Detail.ToString()
                                 && aj.TypeCode != AuditJournalType.UnmodifiedRoot.ToString())
                    .Select(aj => new AuditEntityGroup(
                        aj.TypeCode,
                        aj.EntityType,
                        aj.EntityUid,
                        aj.EntityDescription,
                        corrGroup
                            .Where(d => d.TypeCode == AuditJournalType.Detail.ToString()
                                        && d.EntityUid == aj.EntityUid
                                        && d.PropertyName != null)
                            .Select(d => new AuditPropertyChange(
                                d.PropertyName!,
                                d.PreviousValue,
                                d.CurrentValue))
                            .ToList()))
                    .ToList();

                return new AuditTransactionGroup(
                    first.CorrelationUid,
                    first.Subject,
                    first.UserName,
                    first.OccurredAt,
                    entityGroups);
            })
            .OrderByDescending(t => t.OccurredAt)
            .ToList();
    }

    /// <summary>
    /// Transforms a flat list of audit journal entries into a hierarchical structure,
    /// filtering to only include transactions where a specific entity appears.
    /// </summary>
    /// <param name="entries">Flat list of audit journal entries.</param>
    /// <param name="entityUid">The entity UID to filter by (includes transactions where this entity was modified or is an unmodified root).</param>
    /// <returns>Hierarchical list of transaction groups containing the specified entity.</returns>
    public static List<AuditTransactionGroup> ToHierarchyForEntity(
        this IEnumerable<AuditJournal> entries,
        string entityUid)
    {
        var entryList = entries.ToList();

        // Find correlations where this entity appears (as any type except Detail)
        var relevantCorrelations = entryList
            .Where(aj => aj.EntityUid == entityUid
                         && aj.TypeCode != AuditJournalType.Detail.ToString())
            .Select(aj => aj.CorrelationUid)
            .Distinct()
            .ToHashSet();

        // Filter to only entries in those correlations, then build hierarchy
        return entryList
            .Where(aj => relevantCorrelations.Contains(aj.CorrelationUid))
            .ToHierarchy();
    }
}
