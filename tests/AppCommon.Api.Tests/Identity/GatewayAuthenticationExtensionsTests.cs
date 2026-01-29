using AppCommon.Api.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AppCommon.Api.Tests.Identity;

public class GatewayAuthenticationExtensionsTests
{
    [Fact]
    public void AddGatewayTokenAuthentication_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGatewayTokenAuthentication();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IGatewayTokenEncoder>().ShouldNotBeNull();
        provider.GetService<IAuthenticationSchemeProvider>().ShouldNotBeNull();
    }

    [Fact]
    public async Task AddGatewayTokenAuthentication_SetsDefaultScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGatewayTokenAuthentication();
        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        // Assert
        var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        defaultScheme?.Name.ShouldBe(GatewayTokenOptions.DefaultScheme);
    }

    [Fact]
    public async Task AddGatewayTokenAuthentication_WithCustomScheme_UsesCustomScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGatewayTokenAuthentication("CustomScheme");
        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        // Assert
        var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        defaultScheme?.Name.ShouldBe("CustomScheme");
    }

    [Fact]
    public void AddGatewayTokenAuthentication_WithConfigureOptions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGatewayTokenAuthentication(options =>
        {
            options.HeaderName = "X-Custom-Header";
            options.ValidateExpiration = false;
        });
        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<GatewayTokenOptions>>();

        // Assert
        var options = optionsMonitor.Get(GatewayTokenOptions.DefaultScheme);
        options.HeaderName.ShouldBe("X-Custom-Header");
        options.ValidateExpiration.ShouldBeFalse();
    }

    [Fact]
    public void AddGatewayToken_ToExistingBuilder_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddAuthentication();

        // Act
        builder.AddGatewayToken();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IGatewayTokenEncoder>().ShouldNotBeNull();
    }

    [Fact]
    public async Task AddGatewayToken_WithCustomScheme_RegistersWithCustomScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddAuthentication();

        // Act
        builder.AddGatewayToken("CustomScheme");
        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        // Assert
        var scheme = await schemeProvider.GetSchemeAsync("CustomScheme");
        scheme.ShouldNotBeNull();
    }

    [Fact]
    public void AddGatewayToken_WithConfigureOptions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddAuthentication();

        // Act
        builder.AddGatewayToken(options =>
        {
            options.DevelopmentToken = "dev-token";
        });
        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<GatewayTokenOptions>>();

        // Assert
        var options = optionsMonitor.Get(GatewayTokenOptions.DefaultScheme);
        options.DevelopmentToken.ShouldBe("dev-token");
    }

    [Fact]
    public void AddGatewayTokenAuthentication_ReturnsAuthenticationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var builder = services.AddGatewayTokenAuthentication();

        // Assert
        builder.ShouldBeOfType<AuthenticationBuilder>();
    }
}
