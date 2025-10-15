using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda.Plugins;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.CreationEngine.LoadOrder;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

namespace NexusMods.Games.CreationEngine;

public static class Services
{
    public static IServiceCollection AddCreationEngine(this IServiceCollection services)
    {
        services.AddGame<SkyrimSE.SkyrimSE>();
        services.AddSingleton<ITool>(s => RunGameViaScriptExtenderTool<SkyrimSE.SkyrimSE>.Create(s, KnownPaths.SKSE64Loader));

        services.AddGame<Fallout4.Fallout4>();
        services.AddSingleton<ITool>(s => RunGameViaScriptExtenderTool<Fallout4.Fallout4>.Create(s, KnownPaths.F4SELoader));

        services.AddPluginLoadOrderSql();
        services.AddPluginSortEntryModel();
        
        services.AddValueAdaptor<StringElement, ModKey>(e =>
            {
                if (ModKey.TryFromFileName(e.GetString(), out var key))
                    return key;
                return ModKey.Null;
            }
        );
        return services;
    }
}
