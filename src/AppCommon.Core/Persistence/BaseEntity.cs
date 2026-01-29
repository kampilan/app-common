namespace AppCommon.Core.Persistence;

/// <summary>
/// Base class for all domain entities with identity-based equality.
/// </summary>
/// <typeparam name="TImp">The implementing type for proper equality comparisons.</typeparam>
public abstract class BaseEntity<TImp> : IEntity where TImp : BaseEntity<TImp>
{
    /// <inheritdoc />
    public abstract string GetUid();


    private Type GetUnproxiedType()
    {
        return GetType();
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