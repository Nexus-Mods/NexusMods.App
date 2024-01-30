using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.DarkestDungeon.Installers;

namespace NexusMods.Games.DarkestDungeon;

public static class Services
{
    public static IServiceCollection AddDarkestDungeon(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGame<DarkestDungeon>();
        serviceCollection.AddSingleton<IModInstaller, NativeModInstaller>();
        serviceCollection.AddSingleton<IModInstaller, LooseFilesModInstaller>();
        serviceCollection.AddSingleton<ITypeFinder, TypeFinder>();
        return serviceCollection;
    }

}
