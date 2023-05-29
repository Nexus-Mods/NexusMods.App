using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// Helps with registration of services for Microsoft DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the Nexus Web API to your DI Container's service collection.
    /// </summary>
    public static IServiceCollection AddNexusWebApiNmaIntegration(this IServiceCollection collection, bool apiKeyAuth = false)
    {
        if (apiKeyAuth)
        {
            collection
                .AddAllSingleton<IHttpMessageFactory, ApiKeyMessageFactory>()
                .AddSingleton<IAuthenticatingMessageFactory, ApiKeyMessageFactory>();
        }
        else
        {
            collection
                .AddAllSingleton<IHttpMessageFactory, OAuth2MessageFactory>()
                .AddSingleton<IAuthenticatingMessageFactory, OAuth2MessageFactory>()
                .AddSingleton<OAuth>();
        }

        return collection.AddSingleton<ITypeFinder, TypeFinder>()
            .AddSingleton<LoginManager>();
    }
}
