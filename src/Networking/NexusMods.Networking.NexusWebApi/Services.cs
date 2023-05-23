using Microsoft.Extensions.DependencyInjection;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Helps with registration of services for Microsoft DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the Nexus Web API to your DI Container's service collection.
    /// </summary>
    public static IServiceCollection AddNexusWebApi(this IServiceCollection collection)
    {
        return collection.AddSingleton<Client>();
    }
}
