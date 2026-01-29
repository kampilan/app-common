namespace AppCommon.Core.Persistence;

/// <summary>
/// Marker interface for aggregate root entities.
/// Aggregate roots are the entry point for accessing an aggregate
/// and are responsible for maintaining consistency within the aggregate.
/// </summary>
public interface IRootEntity : IEntity
{
}
