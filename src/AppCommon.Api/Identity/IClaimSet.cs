namespace AppCommon.Api.Identity;

public interface IClaimSet
{
    string? Subject { get; }
    string? Name { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    long? Expiration { get; }
}
