using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI;
using NexusMods.CLI.OptionParsers;
using NexusMods.CLI.Types;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.Verbs;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Helps with registration of services for Microsoft DI container.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the Nexus Web API to your DI Container's service collection.
    /// </summary>
    public static IServiceCollection AddNexusWebApi(this IServiceCollection collection, bool apiKeyAuth = false)
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

        return collection
            .AddSingleton<ITypeFinder, TypeFinder>()
            .AddSingleton<IProtocolHandler, NXMProtocolHandler>()
            .AddSingleton<Client>()
            .AddSingleton<LoginManager>()
            .AddVerb<SetNexusAPIKey>()
            .AddVerb<NexusApiVerify>()
            .AddVerb<NexusGames>()
            .AddVerb<DownloadLinks>()
            .AddVerb<NexusLogin>()
            .AddVerb<NexusLogout>()

            .AddSingleton<IOptionParser<CDNName>, StringOptionParser<CDNName>>()
            .AddSingleton<IOptionParser<ModId>, ULongOptionParser<ModId>>()
            .AddSingleton<IOptionParser<FileId>, ULongOptionParser<FileId>>()
            .AddSingleton<IOptionParser<GameDomain>, StringOptionParser<GameDomain>>();
    }
}
