using fff;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Services for the Loadouts Synchronizers.
/// </summary>
public static class Services
{
    /// <summary>
    /// Registers the services for the Loadouts Synchronizers.
    /// </summary>
    public static IServiceCollection AddLoadoutsSynchronizers(this IServiceCollection services)
    {
        services.AddSynchronizerQueriesSql();
        return services;
    }
}
