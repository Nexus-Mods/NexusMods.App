using Microsoft.Extensions.DependencyInjection;
using NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;
using NexusMods.HyperDuck.BindingConverters;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;

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
            .AddBindingConverter<LoadoutId, ulong>(l => l.Value.Value)
            .AddBindingConverter<LocationId, ushort>(l => l.Value)
            .AddValueAdaptor<ushort, LocationId>(LocationId.From)
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
            .AddGameBackedUpFileModel()
            .AddLoadoutQueriesSql();
    }
}
