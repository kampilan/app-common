using AppCommon.Core.Persistence;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Persistence;

public class BaseEntityTests
{
    #region Test Entity Classes

    private class TestEntity : BaseEntity<TestEntity>
    {
        public string Uid { get; set; } = string.Empty;
        public override string GetUid() => Uid;
    }

    private class DerivedTestEntity : TestEntity
    {
    }

    // Simulates an EF Core proxy class (name contains "Proxy")
    private class TestEntityProxy : TestEntity
    {
    }

    // Simulates a Castle proxy (namespace contains "Proxies")
    private class AnotherEntity : BaseEntity<AnotherEntity>
    {
        public string Uid { get; set; } = string.Empty;
        public override string GetUid() => Uid;
    }

    #endregion

    #region Equals(BaseEntity<TImp>?) Tests

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var entity = new TestEntity { Uid = "123" };

        entity.Equals((TestEntity?)null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        var entity = new TestEntity { Uid = "123" };

        entity.Equals(entity).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithSameUid_ReturnsTrue()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "123" };

        entity1.Equals(entity2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentUid_ReturnsFalse()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "456" };

        entity1.Equals(entity2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDerivedType_SameUid_ReturnsTrue()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new DerivedTestEntity { Uid = "123" };

        entity1.Equals(entity2).ShouldBeTrue();
        entity2.Equals(entity1).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithProxyType_SameUid_ReturnsTrue()
    {
        var entity = new TestEntity { Uid = "123" };
        var proxy = new TestEntityProxy { Uid = "123" };

        entity.Equals(proxy).ShouldBeTrue();
        proxy.Equals(entity).ShouldBeTrue();
    }

    #endregion

    #region Equals(object?) Tests

    [Fact]
    public void EqualsObject_WithNull_ReturnsFalse()
    {
        var entity = new TestEntity { Uid = "123" };

        entity.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void EqualsObject_WithSameReference_ReturnsTrue()
    {
        var entity = new TestEntity { Uid = "123" };

        entity.Equals((object)entity).ShouldBeTrue();
    }

    [Fact]
    public void EqualsObject_WithSameUid_ReturnsTrue()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "123" };

        entity1.Equals((object)entity2).ShouldBeTrue();
    }

    [Fact]
    public void EqualsObject_WithDifferentType_ReturnsFalse()
    {
        var entity = new TestEntity { Uid = "123" };
        var other = "not an entity";

        entity.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void EqualsObject_WithDifferentEntityType_ReturnsFalse()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new AnotherEntity { Uid = "123" };

        // Different generic type parameters, so Equals(object) returns false
        entity1.Equals((object)entity2).ShouldBeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_ReturnsSameValueForSameUid()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "123" };

        entity1.GetHashCode().ShouldBe(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ReturnsDifferentValueForDifferentUid()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "456" };

        entity1.GetHashCode().ShouldNotBe(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquals()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "123" };

        if (entity1.Equals(entity2))
        {
            entity1.GetHashCode().ShouldBe(entity2.GetHashCode());
        }
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsTypeAndUid()
    {
        var entity = new TestEntity { Uid = "abc-123" };

        var result = entity.ToString();

        result.ShouldContain("TestEntity");
        result.ShouldContain("abc-123");
        result.ShouldContain("Uid:");
    }

    #endregion

    #region Operator == Tests

    [Fact]
    public void OperatorEquals_BothNull_ReturnsTrue()
    {
        TestEntity? left = null;
        TestEntity? right = null;

        (left == right).ShouldBeTrue();
    }

    [Fact]
    public void OperatorEquals_LeftNull_ReturnsFalse()
    {
        TestEntity? left = null;
        var right = new TestEntity { Uid = "123" };

        (left == right).ShouldBeFalse();
    }

    [Fact]
    public void OperatorEquals_RightNull_ReturnsFalse()
    {
        var left = new TestEntity { Uid = "123" };
        TestEntity? right = null;

        (left == right).ShouldBeFalse();
    }

    [Fact]
    public void OperatorEquals_SameUid_ReturnsTrue()
    {
        var left = new TestEntity { Uid = "123" };
        var right = new TestEntity { Uid = "123" };

        (left == right).ShouldBeTrue();
    }

    [Fact]
    public void OperatorEquals_DifferentUid_ReturnsFalse()
    {
        var left = new TestEntity { Uid = "123" };
        var right = new TestEntity { Uid = "456" };

        (left == right).ShouldBeFalse();
    }

    #endregion

    #region Operator != Tests

    [Fact]
    public void OperatorNotEquals_BothNull_ReturnsFalse()
    {
        TestEntity? left = null;
        TestEntity? right = null;

        (left != right).ShouldBeFalse();
    }

    [Fact]
    public void OperatorNotEquals_LeftNull_ReturnsTrue()
    {
        TestEntity? left = null;
        var right = new TestEntity { Uid = "123" };

        (left != right).ShouldBeTrue();
    }

    [Fact]
    public void OperatorNotEquals_RightNull_ReturnsTrue()
    {
        var left = new TestEntity { Uid = "123" };
        TestEntity? right = null;

        (left != right).ShouldBeTrue();
    }

    [Fact]
    public void OperatorNotEquals_SameUid_ReturnsFalse()
    {
        var left = new TestEntity { Uid = "123" };
        var right = new TestEntity { Uid = "123" };

        (left != right).ShouldBeFalse();
    }

    [Fact]
    public void OperatorNotEquals_DifferentUid_ReturnsTrue()
    {
        var left = new TestEntity { Uid = "123" };
        var right = new TestEntity { Uid = "456" };

        (left != right).ShouldBeTrue();
    }

    #endregion

    #region Dictionary/HashSet Usage Tests

    [Fact]
    public void Entity_CanBeUsedAsDictionaryKey()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "123" };

        var dict = new Dictionary<TestEntity, string>
        {
            [entity1] = "value"
        };

        // entity2 has same Uid, so should find the same entry
        dict.ContainsKey(entity2).ShouldBeTrue();
        dict[entity2].ShouldBe("value");
    }

    [Fact]
    public void Entity_CanBeUsedInHashSet()
    {
        var entity1 = new TestEntity { Uid = "123" };
        var entity2 = new TestEntity { Uid = "123" };
        var entity3 = new TestEntity { Uid = "456" };

        var set = new HashSet<TestEntity> { entity1 };

        // entity2 has same Uid, so should be considered duplicate
        set.Add(entity2).ShouldBeFalse();
        set.Count.ShouldBe(1);

        // entity3 has different Uid, so should be added
        set.Add(entity3).ShouldBeTrue();
        set.Count.ShouldBe(2);
    }

    #endregion
}
