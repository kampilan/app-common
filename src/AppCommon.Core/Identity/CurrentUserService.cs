namespace AppCommon.Core.Identity;

public class CurrentUserService : ICurrentUserService
{
    /// <summary>
    /// Immutable snapshot of user data. Reference assignment is atomic,
    /// ensuring readers always see consistent state.
    /// </summary>
    private sealed record UserSnapshot(
        string? UserId,
        string? UserName,
        string? Email,
        IReadOnlyList<string> Roles);

    private static readonly UserSnapshot Empty = new(null, null, null, []);

    private volatile UserSnapshot _current = Empty;

    public string? UserId => _current.UserId;
    public string? UserName => _current.UserName;
    public string? Email => _current.Email;
    public IReadOnlyList<string> Roles => _current.Roles;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_current.UserId);

    public void SetUser(string? userId, string? userName, string? email, IEnumerable<string>? roles)
    {
        _current = new UserSnapshot(
            userId,
            userName,
            email,
            roles?.ToList() ?? []);
    }
}
