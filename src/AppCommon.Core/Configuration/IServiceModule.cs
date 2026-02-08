using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppCommon.Core.Configuration;

/// <summary>
/// A module that bundles related service registrations.
/// Implementing classes declare public properties that are populated
/// via <see cref="IConfiguration"/> model binding from the configuration root,
/// then use those values in <see cref="Build"/> to register services.
/// </summary>
public interface IServiceModule
{
    void Build(IServiceCollection services, IConfiguration configuration);
}
