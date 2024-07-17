using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known Game entity related serialization services.
    /// </summary>
    public static IServiceCollection AddLoadoutAbstractions(this IServiceCollection services)
    {
        return services
            .AddLoadoutItemModel()
            .AddLoadoutItemGroupModel()
            .AddLibraryLinkedLoadoutItemModel()
            .AddLoadoutItemWithTargetPathModel()
            .AddLoadoutFileModel()
            // .AddDeletedFileModel()

            // deprecated:
            .AddFileModel()
            .AddStoredFileModel()
            .AddModModel()
            .AddLoadoutModel()
            .AddDeletedFileModel();
    }
}
