using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI;
using NexusMods.CLI.OptionParsers;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Networking.NexusWebApi.Verbs;

namespace NexusMods.Networking.NexusWebApi;

public static class Services
{
    public static IServiceCollection AddNexusWebApi(this IServiceCollection collection)
    {
        return collection.AddAllSingleton<IHttpMessageFactory, ApiKeyMessageFactory>()
            .AddSingleton<Client>()
            .AddVerb<SetNexusAPIKey>(SetNexusAPIKey.Definition)
            .AddVerb<NexusApiVerify>(NexusApiVerify.Definition)
            .AddVerb<NexusGames>(NexusGames.Definition)
            .AddVerb<DownloadLinks>(DownloadLinks.Definition)
            
            .AddSingleton<IOptionParser<CDNName>, StringOptionParser<CDNName>>()
            .AddSingleton<IOptionParser<ModId>, ULongOptionParser<ModId>>()
            .AddSingleton<IOptionParser<FileId>, ULongOptionParser<FileId>>()
            .AddSingleton<IOptionParser<GameDomain>, StringOptionParser<GameDomain>>();
    }
}