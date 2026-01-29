namespace AppCommon.Core.Identity;

public class CurrentUserService : ICurrentUserService
{
    private string? _userId;
    private string? _userName;
    private string? _email;
    private List<string> _roles = [];

    public string? UserId => _userId;
    public string? UserName => _userName;
    public string? Email => _email;
    public IReadOnlyList<string> Roles => _roles;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_userId);

    public void SetUser(string? userId, string? userName, string? email, IEnumerable<string>? roles)
    {
        _userId = userId;
        _userName = userName;
        _email = email;
        _roles = roles?.ToList() ?? [];
    }
}
