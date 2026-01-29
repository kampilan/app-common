namespace AppCommon.Core.Persistence;

/// <summary>
/// Interface for entities that belong to an aggregate and have a root entity.
/// Used by the audit interceptor to track changes to aggregate children
/// and link them to their root entity.
/// </summary>
public interface IAggregateChild : IEntity
{
    /// <summary>
    /// Gets the root entity of the aggregate this entity belongs to.
    /// </summary>
    IRootEntity? GetRoot();
}
