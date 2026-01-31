using AppCommon.Core.Context;
using AppCommon.Core.Persistence;
using AppCommon.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AppCommon.Persistence.Tests.Interceptors;

public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly IRequestContext _requestContext;
    private readonly AuditSaveChangesInterceptor _interceptor;
    private readonly TestDbContext _context;

    public AuditSaveChangesInterceptorTests()
    {
        _requestContext = Substitute.For<IRequestContext>();
        _requestContext.Subject.Returns("user-123");
        _requestContext.UserName.Returns("Test User");
        _requestContext.CorrelationUid.Returns("test-correlation-uid");

        _interceptor = new AuditSaveChangesInterceptor(_requestContext);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        _context = new TestDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityAdded_CreatesCreatedAuditEntry()
    {
        // Arrange
        var entity = new TestAuditedEntity { Name = "Test Entity" };
        _context.TestEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var auditEntries = _context.AuditJournals.ToList();
        auditEntries.ShouldContain(e => e.TypeCode == AuditJournalType.Created.ToString());

        var createdEntry = auditEntries.First(e => e.TypeCode == AuditJournalType.Created.ToString());
        createdEntry.EntityUid.ShouldBe(entity.GetUid());
        createdEntry.Subject.ShouldBe("user-123");
        createdEntry.UserName.ShouldBe("Test User");
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityModified_CreatesUpdatedAuditEntry()
    {
        // Arrange
        var entity = new TestAuditedEntity { Name = "Original Name" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        _context.AuditJournals.RemoveRange(_context.AuditJournals); // Clear previous audit entries
        await _context.SaveChangesAsync();

        // Act
        entity.Name = "Updated Name";
        await _context.SaveChangesAsync();

        // Assert
        var auditEntries = _context.AuditJournals.ToList();
        auditEntries.ShouldContain(e => e.TypeCode == AuditJournalType.Updated.ToString());
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityDeleted_CreatesDeletedAuditEntry()
    {
        // Arrange
        var entity = new TestAuditedEntity { Name = "To Be Deleted" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        _context.AuditJournals.RemoveRange(_context.AuditJournals);
        await _context.SaveChangesAsync();

        // Act
        _context.TestEntities.Remove(entity);
        await _context.SaveChangesAsync();

        // Assert
        var auditEntries = _context.AuditJournals.ToList();
        auditEntries.ShouldContain(e => e.TypeCode == AuditJournalType.Deleted.ToString());
    }

    [Fact]
    public async Task SavingChangesAsync_WithDetailedAudit_CreatesDetailEntries()
    {
        // Arrange
        var entity = new TestAuditedEntity { Name = "Test Entity" };
        _context.TestEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var detailEntries = _context.AuditJournals
            .Where(e => e.TypeCode == AuditJournalType.Detail.ToString())
            .ToList();

        detailEntries.ShouldContain(e => e.PropertyName == "Name" && e.CurrentValue == "Test Entity");
    }

    [Fact]
    public async Task SavingChangesAsync_WithDetailedAudit_RecordsPreviousAndCurrentValues()
    {
        // Arrange
        var entity = new TestAuditedEntity { Name = "Original" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        _context.AuditJournals.RemoveRange(_context.AuditJournals);
        await _context.SaveChangesAsync();

        // Act
        entity.Name = "Updated";
        await _context.SaveChangesAsync();

        // Assert
        var detailEntry = _context.AuditJournals
            .FirstOrDefault(e => e.TypeCode == AuditJournalType.Detail.ToString() && e.PropertyName == "Name");

        detailEntry.ShouldNotBeNull();
        detailEntry.PreviousValue.ShouldBe("Original");
        detailEntry.CurrentValue.ShouldBe("Updated");
    }

    [Fact]
    public async Task SavingChangesAsync_EntityWithoutAuditAttribute_DoesNotCreateAuditEntry()
    {
        // Arrange
        var entity = new TestNonAuditedEntity { Value = 42 };
        _context.NonAuditedEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var auditEntries = _context.AuditJournals
            .Where(e => e.EntityType.Contains("NonAudited"))
            .ToList();
        auditEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task SavingChangesAsync_SetsCorrelationUid()
    {
        // Arrange
        var entity = new TestAuditedEntity { Name = "Test" };
        _context.TestEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var auditEntries = _context.AuditJournals.ToList();
        auditEntries.ShouldAllBe(e => !string.IsNullOrEmpty(e.CorrelationUid));

        // All entries from same save should have same correlation
        var correlationUids = auditEntries.Select(e => e.CorrelationUid).Distinct().ToList();
        correlationUids.Count.ShouldBe(1);
        correlationUids[0].ShouldBe("test-correlation-uid");
    }

    [Fact]
    public async Task SavingChangesAsync_SetsOccurredAt()
    {
        // Arrange
        var beforeSave = DateTime.UtcNow;
        var entity = new TestAuditedEntity { Name = "Test" };
        _context.TestEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();
        var afterSave = DateTime.UtcNow;

        // Assert
        var auditEntry = _context.AuditJournals.First();
        auditEntry.OccurredAt.ShouldBeInRange(beforeSave, afterSave);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenUserNotAuthenticated_UsesAnonymous()
    {
        // Arrange
        _requestContext.Subject.Returns((string?)null);
        _requestContext.UserName.Returns((string?)null);

        var entity = new TestAuditedEntity { Name = "Test" };
        _context.TestEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var auditEntry = _context.AuditJournals.First(e => e.TypeCode == AuditJournalType.Created.ToString());
        auditEntry.Subject.ShouldBe("anonymous");
        auditEntry.UserName.ShouldBe("Anonymous");
    }

    [Fact]
    public async Task SavingChangesAsync_WithAggregateChild_CreatesUnmodifiedRootEntry()
    {
        // Arrange
        var root = new TestRootEntity { Name = "Root" };
        _context.RootEntities.Add(root);
        await _context.SaveChangesAsync();
        _context.AuditJournals.RemoveRange(_context.AuditJournals);
        await _context.SaveChangesAsync();

        var child = new TestChildEntity { Name = "Child", Root = root };
        _context.ChildEntities.Add(child);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var unmodifiedRootEntry = _context.AuditJournals
            .FirstOrDefault(e => e.TypeCode == AuditJournalType.UnmodifiedRoot.ToString());

        unmodifiedRootEntry.ShouldNotBeNull();
        unmodifiedRootEntry.EntityUid.ShouldBe(root.GetUid());
    }

    [Fact]
    public async Task SavingChangesAsync_WhenRootModifiedWithChild_DoesNotCreateUnmodifiedRootEntry()
    {
        // Arrange
        var root = new TestRootEntity { Name = "Root" };
        _context.RootEntities.Add(root);
        await _context.SaveChangesAsync();
        _context.AuditJournals.RemoveRange(_context.AuditJournals);
        await _context.SaveChangesAsync();

        var child = new TestChildEntity { Name = "Child", Root = root };
        _context.ChildEntities.Add(child);
        root.Name = "Modified Root";

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var unmodifiedRootEntries = _context.AuditJournals
            .Where(e => e.TypeCode == AuditJournalType.UnmodifiedRoot.ToString())
            .ToList();

        unmodifiedRootEntries.ShouldBeEmpty();
    }

    [Fact]
    public void SavingChanges_Sync_CreatesAuditEntry()
    {
        // Arrange
        var entity = new TestAuditedEntity { Name = "Sync Test" };
        _context.TestEntities.Add(entity);

        // Act
        _context.SaveChanges();

        // Assert
        var auditEntries = _context.AuditJournals.ToList();
        auditEntries.ShouldContain(e => e.TypeCode == AuditJournalType.Created.ToString());
    }

    [Fact]
    public async Task SavingChangesAsync_TruncatesLongEntityDescription()
    {
        // Arrange
        var longName = new string('x', 600);
        var entity = new TestAuditedEntity { Name = longName };
        _context.TestEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var auditEntry = _context.AuditJournals.First(e => e.TypeCode == AuditJournalType.Created.ToString());
        auditEntry.EntityDescription.Length.ShouldBeLessThanOrEqualTo(500);
    }

    [Fact]
    public async Task SavingChangesAsync_TruncatesLongPropertyValues()
    {
        // Arrange
        var longValue = new string('y', 600);
        var entity = new TestAuditedEntity { Name = longValue };
        _context.TestEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var detailEntry = _context.AuditJournals
            .FirstOrDefault(e => e.TypeCode == AuditJournalType.Detail.ToString() && e.PropertyName == "Name");

        detailEntry.ShouldNotBeNull();
        detailEntry.CurrentValue!.Length.ShouldBeLessThanOrEqualTo(500);
    }

    [Fact]
    public async Task SavingChangesAsync_WithCustomEntityName_UsesCustomName()
    {
        // Arrange
        var entity = new TestCustomNameEntity { Description = "Test" };
        _context.CustomNameEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var auditEntry = _context.AuditJournals.First(e => e.TypeCode == AuditJournalType.Created.ToString());
        auditEntry.EntityType.ShouldBe("CustomEntity");
    }

    [Fact]
    public async Task SavingChangesAsync_WithAuditWriteFalse_DoesNotCreateAuditEntry()
    {
        // Arrange
        var entity = new TestNoWriteAuditEntity { Data = "Test" };
        _context.NoWriteAuditEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var auditEntries = _context.AuditJournals
            .Where(e => e.EntityType.Contains("NoWriteAudit"))
            .ToList();
        auditEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task SavingChangesAsync_WithDetailedFalse_DoesNotCreateDetailEntries()
    {
        // Arrange
        var entity = new TestNoDetailAuditEntity { Info = "Test" };
        _context.NoDetailAuditEntities.Add(entity);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var createdEntry = _context.AuditJournals
            .FirstOrDefault(e => e.TypeCode == AuditJournalType.Created.ToString()
                && e.EntityType.Contains("NoDetailAudit"));
        createdEntry.ShouldNotBeNull();

        var detailEntries = _context.AuditJournals
            .Where(e => e.TypeCode == AuditJournalType.Detail.ToString()
                && e.EntityType.Contains("NoDetailAudit"))
            .ToList();
        detailEntries.ShouldBeEmpty();
    }
}

#region Test Entities and DbContext

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<AuditJournal> AuditJournals => Set<AuditJournal>();
    public DbSet<TestAuditedEntity> TestEntities => Set<TestAuditedEntity>();
    public DbSet<TestNonAuditedEntity> NonAuditedEntities => Set<TestNonAuditedEntity>();
    public DbSet<TestRootEntity> RootEntities => Set<TestRootEntity>();
    public DbSet<TestChildEntity> ChildEntities => Set<TestChildEntity>();
    public DbSet<TestCustomNameEntity> CustomNameEntities => Set<TestCustomNameEntity>();
    public DbSet<TestNoWriteAuditEntity> NoWriteAuditEntities => Set<TestNoWriteAuditEntity>();
    public DbSet<TestNoDetailAuditEntity> NoDetailAuditEntities => Set<TestNoDetailAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditJournal>().HasKey(e => e.Id);
        modelBuilder.Entity<TestAuditedEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestNonAuditedEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestRootEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestChildEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestCustomNameEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestNoWriteAuditEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestNoDetailAuditEntity>().HasKey(e => e.Id);
    }
}

[Audit]
public class TestAuditedEntity : BaseEntity<TestAuditedEntity>
{
    public int Id { get; set; }
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Name { get; set; } = string.Empty;

    public override string GetUid() => Uid;
    public override string ToString() => $"TestAuditedEntity: {Name}";
}

public class TestNonAuditedEntity : BaseEntity<TestNonAuditedEntity>
{
    public int Id { get; set; }
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public int Value { get; set; }

    public override string GetUid() => Uid;
}

[Audit]
public class TestRootEntity : BaseEntity<TestRootEntity>, IRootEntity
{
    public int Id { get; set; }
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Name { get; set; } = string.Empty;

    public override string GetUid() => Uid;
    public override string ToString() => $"TestRootEntity: {Name}";
}

[Audit]
public class TestChildEntity : BaseEntity<TestChildEntity>, IAggregateChild
{
    public int Id { get; set; }
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Name { get; set; } = string.Empty;
    public TestRootEntity? Root { get; set; }

    public override string GetUid() => Uid;
    public IRootEntity? GetRoot() => Root;
    public override string ToString() => $"TestChildEntity: {Name}";
}

[Audit(EntityName = "CustomEntity")]
public class TestCustomNameEntity : BaseEntity<TestCustomNameEntity>
{
    public int Id { get; set; }
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Description { get; set; } = string.Empty;

    public override string GetUid() => Uid;
}

[Audit(Write = false)]
public class TestNoWriteAuditEntity : BaseEntity<TestNoWriteAuditEntity>
{
    public int Id { get; set; }
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Data { get; set; } = string.Empty;

    public override string GetUid() => Uid;
}

[Audit(Detailed = false)]
public class TestNoDetailAuditEntity : BaseEntity<TestNoDetailAuditEntity>
{
    public int Id { get; set; }
    public string Uid { get; set; } = Ulid.NewUlid().ToString();
    public string Info { get; set; } = string.Empty;

    public override string GetUid() => Uid;
    public override string ToString() => $"TestNoDetailAuditEntity: {Info}";
}

#endregion
