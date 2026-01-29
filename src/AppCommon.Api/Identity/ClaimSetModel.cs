namespace AppCommon.Api.Identity;

public class ClaimSetModel : IClaimSet
{
    public string? Subject { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public List<string>? RolesList { get; set; }
    public long? Expiration { get; set; }

    IEnumerable<string> IClaimSet.Roles => RolesList ?? [];
}
