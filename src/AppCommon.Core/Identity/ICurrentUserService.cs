namespace AppCommon.Core.Identity;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    void SetUser(string? userId, string? userName, string? email, IEnumerable<string>? roles);
}
