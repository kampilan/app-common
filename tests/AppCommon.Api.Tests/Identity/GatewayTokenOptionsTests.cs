using AppCommon.Api.Identity;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Identity;

public class GatewayTokenOptionsTests
{
    [Fact]
    public void DefaultScheme_IsGatewayToken()
    {
        GatewayTokenOptions.DefaultScheme.ShouldBe("GatewayToken");
    }

    [Fact]
    public void DefaultHeaderName_IsXGatewayToken()
    {
        GatewayTokenOptions.DefaultHeaderName.ShouldBe("X-Gateway-Token");
    }

    [Fact]
    public void HeaderName_DefaultsToDefaultHeaderName()
    {
        var options = new GatewayTokenOptions();
        options.HeaderName.ShouldBe(GatewayTokenOptions.DefaultHeaderName);
    }

    [Fact]
    public void ValidateSignature_DefaultsToFalse()
    {
        var options = new GatewayTokenOptions();
        options.ValidateSignature.ShouldBeFalse();
    }

    [Fact]
    public void ValidateExpiration_DefaultsToTrue()
    {
        var options = new GatewayTokenOptions();
        options.ValidateExpiration.ShouldBeTrue();
    }

    [Fact]
    public void ClockSkew_DefaultsToFiveMinutes()
    {
        var options = new GatewayTokenOptions();
        options.ClockSkew.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void SigningKey_DefaultsToNull()
    {
        var options = new GatewayTokenOptions();
        options.SigningKey.ShouldBeNull();
    }

    [Fact]
    public void DevelopmentToken_DefaultsToNull()
    {
        var options = new GatewayTokenOptions();
        options.DevelopmentToken.ShouldBeNull();
    }
}
