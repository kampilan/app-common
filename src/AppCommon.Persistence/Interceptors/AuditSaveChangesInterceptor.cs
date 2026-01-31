using System.Reflection;
using AppCommon.Core.Context;
using AppCommon.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AppCommon.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that captures entity changes and creates audit journal entries.
/// </summary>
/// <remarks>
/// <para>
/// <b>How it works:</b> Intercepts <c>SaveChanges</c>/<c>SaveChangesAsync</c> and scans the
/// ChangeTracker for entities marked with <see cref="AuditAttribute"/>. Creates
/// <see cref="AuditJournal"/> entries recording what changed, when, and by whom.
/// </para>
/// <para>
/// <b>User context:</b> This interceptor reads from <see cref="IRequestContext"/> to
/// determine who made the changes:
/// <list type="bullet">
/// <item><description><see cref="AuditJournal.Subject"/> = <see cref="IRequestContext.Subject"/> (or "anonymous")</description></item>
/// <item><description><see cref="AuditJournal.UserName"/> = <see cref="IRequestContext.UserName"/> (or "Anonymous")</description></item>
/// <item><description><see cref="AuditJournal.CorrelationUid"/> = <see cref="IRequestContext.CorrelationUid"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Integration:</b> User info is automatically available when any ASP.NET Core authentication
/// handler populates <c>HttpContext.User</c>. No manual setup required beyond calling
/// <c>app.UseAuthentication()</c> before endpoints.
/// </para>
/// <para>
/// <b>Registration:</b>
/// <code>
/// services.AddRequestContext();  // Required - from AppCommon.Api
/// services.AddScoped&lt;AuditSaveChangesInterceptor&gt;();
/// services.AddDbContext&lt;MyDbContext&gt;((sp, opt) =&gt;
///     opt.AddInterceptors(sp.GetRequiredService&lt;AuditSaveChangesInterceptor&gt;()));
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IRequestContext"/>
/// <seealso cref="AuditJournal"/>
/// <seealso cref="AuditAttribute"/>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IRequestContext _requestContext;

    /// <summary>
    /// Initializes a new instance of the interceptor.
    /// </summary>
    /// <param name="requestContext">
    /// The request context providing user identity and correlation information.
    /// </param>
    public AuditSaveChangesInterceptor(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not DbContext context)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditEntries = CreateAuditEntries(context);

        if (auditEntries.Count > 0)
        {
            context.AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not DbContext context)
            return base.SavingChanges(eventData, result);

        var auditEntries = CreateAuditEntries(context);

        if (auditEntries.Count > 0)
        {
            context.AddRange(auditEntries);
        }

        return base.SavingChanges(eventData, result);
    }

    private List<AuditJournal> CreateAuditEntries(DbContext context)
    {
        var journals = new List<AuditJournal>();
        var correlationUid = _requestContext.CorrelationUid;
        var occurredAt = DateTime.UtcNow;
        var subject = _requestContext.Subject ?? "anonymous";
        var userName = _requestContext.UserName ?? "Anonymous";

        // Track which root entities were directly modified
        var modifiedRootUids = new HashSet<string>();
        // Track roots that need UnmodifiedRoot entries (from aggregate children)
        var unmodifiedRoots = new Dictionary<string, IRootEntity>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Skip non-IEntity types
            if (entry.Entity is not IEntity entity)
                continue;

            // Skip AuditJournal entities to avoid infinite recursion
            if (entry.Entity is AuditJournal)
                continue;

            // Check for [Audit] attribute
            var auditAttribute = entry.Entity.GetType().GetCustomAttribute<AuditAttribute>(true);
            if (auditAttribute == null || !auditAttribute.Write)
                continue;

            // Track if this is a modified root entity
            if (entry.Entity is IRootEntity rootEntity && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                modifiedRootUids.Add(rootEntity.GetUid());
            }

            // For aggregate children, track their root for potential UnmodifiedRoot entry
            if (entry.Entity is IAggregateChild aggregateChild && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var root = aggregateChild.GetRoot();
                if (root != null && !unmodifiedRoots.ContainsKey(root.GetUid()))
                {
                    unmodifiedRoots[root.GetUid()] = root;
                }
            }

            var entityType = auditAttribute.EntityName ?? entry.Entity.GetType().FullName!;
            var entityDescription = entry.Entity.ToString() ?? string.Empty;
            if (entityDescription.Length > 500)
                entityDescription = entityDescription[..500];

            switch (entry.State)
            {
                case EntityState.Added:
                    journals.Add(CreateJournalEntry(
                        correlationUid, subject, userName, occurredAt,
                        AuditJournalType.Created, entityType, entity.GetUid(), entityDescription));

                    if (auditAttribute.Detailed)
                    {
                        journals.AddRange(CreateDetailEntries(
                            entry, correlationUid, subject, userName, occurredAt,
                            entityType, entity.GetUid(), entityDescription, isNew: true));
                    }
                    break;

                case EntityState.Modified:
                    journals.Add(CreateJournalEntry(
                        correlationUid, subject, userName, occurredAt,
                        AuditJournalType.Updated, entityType, entity.GetUid(), entityDescription));

                    if (auditAttribute.Detailed)
                    {
                        journals.AddRange(CreateDetailEntries(
                            entry, correlationUid, subject, userName, occurredAt,
                            entityType, entity.GetUid(), entityDescription, isNew: false));
                    }
                    break;

                case EntityState.Deleted:
                    journals.Add(CreateJournalEntry(
                        correlationUid, subject, userName, occurredAt,
                        AuditJournalType.Deleted, entityType, entity.GetUid(), entityDescription));
                    break;
            }
        }

        // Add UnmodifiedRoot entries for roots that weren't directly modified
        foreach (var (rootUid, root) in unmodifiedRoots)
        {
            if (modifiedRootUids.Contains(rootUid))
                continue;

            var rootAuditAttribute = root.GetType().GetCustomAttribute<AuditAttribute>(true);
            var rootEntityType = rootAuditAttribute?.EntityName ?? root.GetType().FullName!;
            var rootDescription = root.ToString() ?? string.Empty;
            if (rootDescription.Length > 500)
                rootDescription = rootDescription[..500];

            journals.Add(CreateJournalEntry(
                correlationUid, subject, userName, occurredAt,
                AuditJournalType.UnmodifiedRoot, rootEntityType, rootUid, rootDescription));
        }

        return journals;
    }

    private static AuditJournal CreateJournalEntry(
        string correlationUid,
        string subject,
        string userName,
        DateTime occurredAt,
        AuditJournalType type,
        string entityType,
        string entityUid,
        string entityDescription)
    {
        return new AuditJournal
        {
            CorrelationUid = correlationUid,
            Subject = subject,
            UserName = userName,
            OccurredAt = occurredAt,
            TypeCode = type.ToString(),
            EntityType = entityType,
            EntityUid = entityUid,
            EntityDescription = entityDescription
        };
    }

    private static IEnumerable<AuditJournal> CreateDetailEntries(
        EntityEntry entry,
        string correlationUid,
        string subject,
        string userName,
        DateTime occurredAt,
        string entityType,
        string entityUid,
        string entityDescription,
        bool isNew)
    {
        foreach (var property in entry.Properties)
        {
            // Skip properties that haven't changed (for updates)
            if (!isNew && !property.IsModified)
                continue;

            // Skip navigation properties and shadow properties
            if (property.Metadata.IsShadowProperty())
                continue;

            // Skip the Id property for new entities (it's always 0 initially)
            if (isNew && property.Metadata.Name == "Id")
                continue;

            var previousValue = isNew ? null : TruncateValue(property.OriginalValue);
            var currentValue = TruncateValue(property.CurrentValue);

            // Skip if values are the same (can happen with complex comparisons)
            if (!isNew && previousValue == currentValue)
                continue;

            yield return new AuditJournal
            {
                CorrelationUid = correlationUid,
                Subject = subject,
                UserName = userName,
                OccurredAt = occurredAt,
                TypeCode = AuditJournalType.Detail.ToString(),
                EntityType = entityType,
                EntityUid = entityUid,
                EntityDescription = entityDescription,
                PropertyName = property.Metadata.Name,
                PreviousValue = previousValue,
                CurrentValue = currentValue
            };
        }
    }

    private static string? TruncateValue(object? value)
    {
        if (value == null)
            return null;

        var str = value.ToString() ?? string.Empty;
        return str.Length > 500 ? str[..500] : str;
    }
}
