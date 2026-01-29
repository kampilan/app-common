namespace AppCommon.Api.Identity;

public interface IGatewayTokenEncoder
{
    Task<IClaimSet> DecodeAsync(string token);
}
