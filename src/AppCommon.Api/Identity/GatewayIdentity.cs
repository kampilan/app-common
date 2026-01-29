using System.Security.Claims;

namespace AppCommon.Api.Identity;

public class GatewayIdentity : ClaimsIdentity
{
    public GatewayIdentity(IClaimSet claimSet, string authenticationType = GatewayTokenOptions.DefaultScheme)
        : base(authenticationType)
    {
        AddClaimIfPresent(ClaimTypes.NameIdentifier, claimSet.Subject);
        AddClaimIfPresent(ClaimTypes.Name, claimSet.Name);
        AddClaimIfPresent(ClaimTypes.Email, claimSet.Email);

        foreach (var role in claimSet.Roles)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    private void AddClaimIfPresent(string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            AddClaim(new Claim(type, value));
        }
    }
}
