namespace AppCommon.Core.Persistence;

/// <summary>
/// Base class for all domain entities with identity-based equality.
/// </summary>
/// <typeparam name="TImp">The implementing type for proper equality comparisons.</typeparam>
public abstract class BaseEntity<TImp> : IEntity where TImp : BaseEntity<TImp>
{
    /// <inheritdoc />
    public abstract string GetUid();


    /// <summary>
    /// Gets the actual entity type, unwrapping EF Core lazy-loading proxies if present.
    /// </summary>
    private Type GetUnproxiedType()
    {
        var type = GetType();

        // EF Core proxies inherit from the entity type, so the base type is the real entity
        // Proxy type names contain "Proxy" or "Castle" (Castle.Proxies namespace)
        if (type.Namespace?.Contains("Proxies") == true || type.Name.Contains("Proxy"))
            return type.BaseType ?? type;

        return type;
    }


    public virtual bool Equals(BaseEntity<TImp>? other)
    {

        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Equals(GetUid(), other.GetUid()))
        {

            var typeOther = other.GetUnproxiedType();
            var typeThis = GetUnproxiedType();

            return (typeThis.IsAssignableFrom(typeOther)) || (typeOther.IsAssignableFrom(typeThis));

        }

        return false;

    }



    public override bool Equals(object? other)
    {
        if (other is BaseEntity<TImp> a)
            return Equals(a);

        return false;

    }

    public override int GetHashCode()
    {
        return GetUid().GetHashCode();
    }



    public override string ToString()
    {
        var s = $"{GetType().FullName} - Uid: {GetUid()}";
        return s;
    }


    public static bool operator ==(BaseEntity<TImp>? left, BaseEntity<TImp>? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(BaseEntity<TImp>? left, BaseEntity<TImp>? right)
    {
        return !(left == right);
    }    
    
    
}