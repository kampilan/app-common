using System.Diagnostics;
using System.Security.Claims;
using AppCommon.Core.Context;
using Microsoft.AspNetCore.Http;

namespace AppCommon.Api.Context;

/// <summary>
/// Default implementation of <see cref="IRequestContext"/> that reads from <see cref="HttpContext.User"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation lazily reads from the current HTTP context's <c>ClaimsPrincipal</c>.
/// Any authentication handler that populates <c>HttpContext.User</c> will automatically
/// be reflected in the properties of this class.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class reads from <c>IHttpContextAccessor</c> on each property access,
/// which is safe for the scoped lifetime of an HTTP request.
/// </para>
/// <para>
/// <b>Non-HTTP scenarios:</b> When <c>HttpContext</c> is null (e.g., in background services),
/// user properties return null/empty and <c>IsAuthenticated</c> returns false.
/// <c>CorrelationUid</c> will still return a valid identifier (from Activity.TraceId or a generated ULID).
/// </para>
/// </remarks>
/// <seealso cref="IRequestContext"/>
public class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _correlationUid;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestContext"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public RequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string? Subject => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc />
    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc />
    public string? UserEmail => User?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc />
    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? [];

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public string CorrelationUid =>
        Activity.Current?.TraceId.ToString() is { Length: > 0 } traceId
            ? traceId
            : (_correlationUid ??= Ulid.NewUlid().ToString());
}
