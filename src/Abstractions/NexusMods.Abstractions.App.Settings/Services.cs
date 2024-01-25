using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.App.Settings;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known DataModel entity related serialization services.
    /// </summary>
    public static IServiceCollection AddDataModelSettings(this IServiceCollection services)
    {
        services.AddSingleton<GlobalSettingsManager>();
        return services;
    }
}
