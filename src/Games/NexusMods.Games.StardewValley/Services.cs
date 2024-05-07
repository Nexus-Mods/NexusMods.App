using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.RunGameTools;
using NexusMods.Games.StardewValley.WebAPI;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.StardewValley;

public static class Services
{
    public static IServiceCollection AddStardewValley(this IServiceCollection services)
    {
        services
            .AddGame<StardewValley>()
            .AddSingleton<ITool, SmapiRunGameTool>()

            // Installers
            .AddSingleton<SMAPIInstaller>()
            .AddSingleton<SMAPIModInstaller>()

            // Diagnostics
            .AddSingleton<DependencyDiagnosticEmitter>()
            .AddSingleton<MissingSMAPIEmitter>()
            .AddSingleton<SMAPIModDatabaseCompatibilityDiagnosticEmitter>()
            .AddSingleton<SMAPIGameVersionDiagnosticEmitter>()
            .AddSingleton<VersionDiagnosticEmitter>()

            // Attributes
            .AddAttributeCollection(typeof(SMAPIMarker))
            .AddAttributeCollection(typeof(SMAPIModMarker))
            .AddAttributeCollection(typeof(SMAPIManifestMetadata))
            .AddAttributeCollection(typeof(SMAPIModDatabaseMarker))

            // Misc
            .AddSingleton<ISMAPIWebApi, SMAPIWebApi>();

        return services;
    }
}
