using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;

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
        services = services
            .AddLoadoutItemModel()
            .AddLoadoutItemGroupModel()
            .AddLoadoutGameFilesGroupModel()
            .AddLoadoutOverridesGroupModel()
            .AddLibraryLinkedLoadoutItemModel()
            .AddLoadoutItemWithTargetPathModel()
            .AddLoadoutFileModel()
            .AddDeletedFileModel()

            // deprecated:
            .AddFileModel()
            .AddStoredFileModel()
            .AddModModel()
            .AddLoadoutModel();

        return Files.DeletedFileExtensions.AddDeletedFileModel(services);
    }
}
