namespace AppCommon.Core.Persistence;

/// <summary>
/// Base interface for all domain entities with a unique identifier.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    string GetUid();
}
