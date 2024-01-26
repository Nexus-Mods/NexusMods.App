using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Helps with registration of services for Microsoft DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the Nexus Web API to your DI Container's service collection.
    /// </summary>
    public static IServiceCollection AddNexusWebApi(this IServiceCollection collection, bool? apiKeyAuth = null)
    {
        collection.AddLoginVerbs();

        apiKeyAuth ??= Environment.GetEnvironmentVariable(ApiKeyMessageFactory.NexusApiKeyEnvironmentVariable) != null;

        if (apiKeyAuth!.Value)
        {
            collection
                .AddAllSingleton<IHttpMessageFactory, ApiKeyMessageFactory>()
                .AddSingleton<IAuthenticatingMessageFactory, ApiKeyMessageFactory>();
        }
        else
        {
            collection
                .AddAllSingleton<IHttpMessageFactory, OAuth2MessageFactory>()
                .AddSingleton<IAuthenticatingMessageFactory, OAuth2MessageFactory>();
        }
        collection.AddSingleton<OAuth>();
        collection.AddSingleton<IIDGenerator, IDGenerator>();
        return collection.AddSingleton<ITypeFinder, TypeFinder>()
            .AddAllSingleton<ILoginManager, LoginManager>()
            .AddAllSingleton<INexusApiClient, NexusApiClient>()
            .AddNexusApiVerbs();
    }
}
