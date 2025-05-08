using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.MnemonicDB.Abstractions;

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
            .AddLoadoutModel()
            .AddLoadoutItemModel()
            .AddLoadoutItemGroupModel()
            .AddLoadoutGameFilesGroupModel()
            .AddLoadoutOverridesGroupModel()
            .AddLibraryLinkedLoadoutItemModel()
            .AddLoadoutItemWithTargetPathModel()
            .AddLoadoutFileModel()
            .AddLoadoutSnapshotModel()
            .AddDeletedFileModel()
            .AddCollectionGroupModel()
            .AddSortOrderModel()
            .AddGameBackedUpFileModel();
    }
}
