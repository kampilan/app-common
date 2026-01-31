namespace AppCommon.Core.Context;

/// <summary>
/// Provides access to contextual information about the current request/operation.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a unified way to access user identity and correlation information
/// across the application, regardless of the source (HTTP request, message queue, background job).
/// </para>
/// <para>
/// <b>For HTTP requests:</b> Properties are populated from <c>HttpContext.User</c> claims.
/// Any authentication handler (JWT, cookies, gateway tokens, etc.) that populates the
/// <c>ClaimsPrincipal</c> will automatically be reflected here.
/// </para>
/// <para>
/// <b>For background services:</b> Returns anonymous/default values since there's no HTTP context.
/// </para>
/// <para>
/// <b>Components that use IRequestContext:</b>
/// <list type="bullet">
/// <item><description><c>LoggingBehavior</c> - includes Subject in all mediator request logs</description></item>
/// <item><description><c>AuditSaveChangesInterceptor</c> - records who made changes and correlates entries</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Registration:</b>
/// <code>
/// services.AddRequestContext(); // From AppCommon.Api
/// </code>
/// </para>
/// </remarks>
public interface IRequestContext
{
    /// <summary>
    /// Gets the unique identifier of the current user (from JWT "sub" claim or ClaimTypes.NameIdentifier).
    /// Returns null if not authenticated.
    /// </summary>
    string? Subject { get; }

    /// <summary>
    /// Gets the display name of the current user (from JWT "name" claim or ClaimTypes.Name).
    /// Returns null if not authenticated or name not provided.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the email address of the current user (from JWT "email" claim or ClaimTypes.Email).
    /// Returns null if not authenticated or email not provided.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Gets the roles assigned to the current user (from ClaimTypes.Role claims).
    /// Returns an empty list if not authenticated or no roles assigned.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the correlation identifier for the current operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <c>Activity.Current.TraceId</c> if available (for distributed tracing integration),
    /// otherwise generates a ULID that remains consistent for the lifetime of this scope.
    /// </para>
    /// <para>
    /// This value is used by <c>AuditSaveChangesInterceptor</c> to correlate all changes
    /// made within a single operation.
    /// </para>
    /// </remarks>
    string CorrelationUid { get; }
}
