using AppCommon.Core.Persistence;
using Shouldly;
using Xunit;

namespace AppCommon.Persistence.Tests;

public class AuditJournalExtensionsTests
{
    private static AuditJournal CreateEntry(
        string correlationUid,
        string subject,
        string userName,
        DateTime occurredAt,
        string typeCode,
        string entityType,
        string entityUid,
        string entityDescription,
        string? propertyName = null,
        string? previousValue = null,
        string? currentValue = null)
    {
        return new AuditJournal
        {
            CorrelationUid = correlationUid,
            Subject = subject,
            UserName = userName,
            OccurredAt = occurredAt,
            TypeCode = typeCode,
            EntityType = entityType,
            EntityUid = entityUid,
            EntityDescription = entityDescription,
            PropertyName = propertyName,
            PreviousValue = previousValue,
            CurrentValue = currentValue
        };
    }

    #region ToHierarchy Tests

    [Fact]
    public void ToHierarchy_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var entries = new List<AuditJournal>();

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ToHierarchy_SingleTransaction_SingleEntity_ReturnsOneTransactionGroup()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User One", occurredAt,
                AuditJournalType.Created.ToString(), "Client", "client-uid-1", "Client: Test")
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result.Count.ShouldBe(1);

        var transaction = result[0];
        transaction.CorrelationUid.ShouldBe("corr-1");
        transaction.Subject.ShouldBe("user-1");
        transaction.UserName.ShouldBe("User One");
        transaction.OccurredAt.ShouldBe(occurredAt);
        transaction.Entities.Count.ShouldBe(1);

        var entity = transaction.Entities[0];
        entity.TypeCode.ShouldBe(AuditJournalType.Created.ToString());
        entity.EntityType.ShouldBe("Client");
        entity.EntityUid.ShouldBe("client-uid-1");
        entity.EntityDescription.ShouldBe("Client: Test");
        entity.Properties.ShouldBeEmpty();
    }

    [Fact]
    public void ToHierarchy_SingleTransaction_MultipleEntities_GroupsCorrectly()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User One", occurredAt,
                AuditJournalType.Created.ToString(), "Client", "client-1", "Client: A"),
            CreateEntry("corr-1", "user-1", "User One", occurredAt,
                AuditJournalType.Created.ToString(), "Contact", "contact-1", "Contact: B")
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Entities.Count.ShouldBe(2);
        result[0].Entities.ShouldContain(e => e.EntityUid == "client-1");
        result[0].Entities.ShouldContain(e => e.EntityUid == "contact-1");
    }

    [Fact]
    public void ToHierarchy_MultipleTransactions_OrderedByOccurredAtDescending()
    {
        // Arrange
        var oldest = DateTime.UtcNow.AddHours(-2);
        var middle = DateTime.UtcNow.AddHours(-1);
        var newest = DateTime.UtcNow;

        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-old", "user-1", "User", oldest,
                AuditJournalType.Created.ToString(), "Client", "client-1", "Old"),
            CreateEntry("corr-new", "user-1", "User", newest,
                AuditJournalType.Created.ToString(), "Client", "client-3", "New"),
            CreateEntry("corr-mid", "user-1", "User", middle,
                AuditJournalType.Created.ToString(), "Client", "client-2", "Middle")
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result.Count.ShouldBe(3);
        result[0].CorrelationUid.ShouldBe("corr-new");
        result[1].CorrelationUid.ShouldBe("corr-mid");
        result[2].CorrelationUid.ShouldBe("corr-old");
    }

    [Fact]
    public void ToHierarchy_WithDetailEntries_AttachesPropertiesToCorrectEntity()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Updated.ToString(), "Client", "client-1", "Client: Test"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "client-1", "Client: Test",
                "Name", "Old Name", "New Name"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "client-1", "Client: Test",
                "Email", "old@test.com", "new@test.com")
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Entities.Count.ShouldBe(1);

        var entity = result[0].Entities[0];
        entity.Properties.Count.ShouldBe(2);

        entity.Properties.ShouldContain(p =>
            p.PropertyName == "Name" &&
            p.PreviousValue == "Old Name" &&
            p.CurrentValue == "New Name");

        entity.Properties.ShouldContain(p =>
            p.PropertyName == "Email" &&
            p.PreviousValue == "old@test.com" &&
            p.CurrentValue == "new@test.com");
    }

    [Fact]
    public void ToHierarchy_WithMultipleEntitiesAndDetails_AttachesPropertiesToCorrectEntities()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            // Entity 1 - Updated with 1 property change
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Updated.ToString(), "Client", "client-1", "Client: A"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "client-1", "Client: A",
                "Name", "A1", "A2"),

            // Entity 2 - Created with 2 property changes
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Created.ToString(), "Contact", "contact-1", "Contact: B"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Contact", "contact-1", "Contact: B",
                "FirstName", null, "John"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Contact", "contact-1", "Contact: B",
                "LastName", null, "Doe")
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Entities.Count.ShouldBe(2);

        var client = result[0].Entities.First(e => e.EntityUid == "client-1");
        client.Properties.Count.ShouldBe(1);
        client.Properties[0].PropertyName.ShouldBe("Name");

        var contact = result[0].Entities.First(e => e.EntityUid == "contact-1");
        contact.Properties.Count.ShouldBe(2);
    }

    [Fact]
    public void ToHierarchy_DetailEntriesWithNullPropertyName_AreExcluded()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Updated.ToString(), "Client", "client-1", "Client: Test"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "client-1", "Client: Test",
                null, null, null), // Detail with null PropertyName
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "client-1", "Client: Test",
                "ValidProp", "old", "new")
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        var entity = result[0].Entities[0];
        entity.Properties.Count.ShouldBe(1);
        entity.Properties[0].PropertyName.ShouldBe("ValidProp");
    }

    [Fact]
    public void ToHierarchy_IncludesCreatedUpdatedDeleted_ExcludesUnmodifiedRoot()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Created.ToString(), "Client", "client-1", "Created"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Updated.ToString(), "Client", "client-2", "Updated"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Deleted.ToString(), "Client", "client-3", "Deleted"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.UnmodifiedRoot.ToString(), "Client", "client-4", "Unmodified")
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result[0].Entities.Count.ShouldBe(3);
        result[0].Entities.ShouldContain(e => e.TypeCode == AuditJournalType.Created.ToString());
        result[0].Entities.ShouldContain(e => e.TypeCode == AuditJournalType.Updated.ToString());
        result[0].Entities.ShouldContain(e => e.TypeCode == AuditJournalType.Deleted.ToString());
        result[0].Entities.ShouldNotContain(e => e.TypeCode == AuditJournalType.UnmodifiedRoot.ToString());
    }

    [Fact]
    public void ToHierarchy_DetailEntriesWithoutMatchingEntity_AreIgnored()
    {
        // Arrange - Detail entry for entity that doesn't have a non-Detail entry
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Created.ToString(), "Client", "client-1", "Client: Test"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "orphan-entity", "Orphan",
                "PropName", "old", "new") // Detail for non-existent entity
        };

        // Act
        var result = entries.ToHierarchy();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Entities.Count.ShouldBe(1);
        result[0].Entities[0].EntityUid.ShouldBe("client-1");
        result[0].Entities[0].Properties.ShouldBeEmpty(); // No orphan details attached
    }

    #endregion

    #region ToHierarchyForEntity Tests

    [Fact]
    public void ToHierarchyForEntity_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var entries = new List<AuditJournal>();

        // Act
        var result = entries.ToHierarchyForEntity("any-uid");

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ToHierarchyForEntity_EntityNotFound_ReturnsEmptyList()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Created.ToString(), "Client", "client-1", "Client: Test")
        };

        // Act
        var result = entries.ToHierarchyForEntity("non-existent-uid");

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ToHierarchyForEntity_FindsTransactionsContainingEntity()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            // Transaction 1 - Contains target entity
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Created.ToString(), "Client", "target-entity", "Target"),

            // Transaction 2 - Does NOT contain target entity
            CreateEntry("corr-2", "user-1", "User", occurredAt.AddMinutes(1),
                AuditJournalType.Created.ToString(), "Client", "other-entity", "Other"),

            // Transaction 3 - Contains target entity (updated)
            CreateEntry("corr-3", "user-1", "User", occurredAt.AddMinutes(2),
                AuditJournalType.Updated.ToString(), "Client", "target-entity", "Target Updated")
        };

        // Act
        var result = entries.ToHierarchyForEntity("target-entity");

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(t => t.CorrelationUid == "corr-1");
        result.ShouldContain(t => t.CorrelationUid == "corr-3");
        result.ShouldNotContain(t => t.CorrelationUid == "corr-2");
    }

    [Fact]
    public void ToHierarchyForEntity_IncludesAllEntitiesInMatchingTransactions()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            // Transaction with target entity and another entity
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Updated.ToString(), "Client", "target-entity", "Target"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Created.ToString(), "Contact", "contact-1", "Contact: New")
        };

        // Act
        var result = entries.ToHierarchyForEntity("target-entity");

        // Assert
        result.Count.ShouldBe(1);
        result[0].Entities.Count.ShouldBe(2);
        result[0].Entities.ShouldContain(e => e.EntityUid == "target-entity");
        result[0].Entities.ShouldContain(e => e.EntityUid == "contact-1");
    }

    [Fact]
    public void ToHierarchyForEntity_DoesNotMatchOnDetailEntries()
    {
        // Arrange - Entity only appears as a Detail entry, not as a non-Detail entry
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Updated.ToString(), "Client", "client-1", "Client"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "detail-only-entity", "Detail",
                "PropName", "old", "new")
        };

        // Act
        var result = entries.ToHierarchyForEntity("detail-only-entity");

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ToHierarchyForEntity_FindsTransactionViaUnmodifiedRoot_ButExcludesItFromOutput()
    {
        // Arrange - UnmodifiedRoot is used for querying but excluded from output
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            // Transaction where target entity is an UnmodifiedRoot
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.UnmodifiedRoot.ToString(), "Client", "target-entity", "Root"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Created.ToString(), "Contact", "contact-1", "Child Contact")
        };

        // Act
        var result = entries.ToHierarchyForEntity("target-entity");

        // Assert - Transaction is found via UnmodifiedRoot, but only the actual change is in output
        result.Count.ShouldBe(1);
        result[0].Entities.Count.ShouldBe(1);
        result[0].Entities[0].EntityUid.ShouldBe("contact-1");
        result[0].Entities.ShouldNotContain(e => e.TypeCode == AuditJournalType.UnmodifiedRoot.ToString());
    }

    [Fact]
    public void ToHierarchyForEntity_ResultIsOrderedByOccurredAtDescending()
    {
        // Arrange
        var oldest = DateTime.UtcNow.AddHours(-2);
        var newest = DateTime.UtcNow;

        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-old", "user-1", "User", oldest,
                AuditJournalType.Created.ToString(), "Client", "target", "Old"),
            CreateEntry("corr-new", "user-1", "User", newest,
                AuditJournalType.Updated.ToString(), "Client", "target", "New")
        };

        // Act
        var result = entries.ToHierarchyForEntity("target");

        // Assert
        result.Count.ShouldBe(2);
        result[0].CorrelationUid.ShouldBe("corr-new");
        result[1].CorrelationUid.ShouldBe("corr-old");
    }

    [Fact]
    public void ToHierarchyForEntity_IncludesDetailEntriesForMatchingTransaction()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var entries = new List<AuditJournal>
        {
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Updated.ToString(), "Client", "target-entity", "Target"),
            CreateEntry("corr-1", "user-1", "User", occurredAt,
                AuditJournalType.Detail.ToString(), "Client", "target-entity", "Target",
                "Name", "Old", "New")
        };

        // Act
        var result = entries.ToHierarchyForEntity("target-entity");

        // Assert
        result.Count.ShouldBe(1);
        var entity = result[0].Entities[0];
        entity.Properties.Count.ShouldBe(1);
        entity.Properties[0].PropertyName.ShouldBe("Name");
    }

    #endregion
}
