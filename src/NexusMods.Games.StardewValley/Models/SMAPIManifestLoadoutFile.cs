using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Games.StardewValley.Models;

[Include<LoadoutFile>]
public partial class SMAPIManifestLoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.StardewValley.SMAPIManifestLoadoutFile";

    public static readonly MarkerAttribute ManifestFile = new(Namespace, nameof(ManifestFile)) { IsIndexed = true };

    public static IEnumerable<ReadOnly> GetAllInLoadout(IDb db, LoadoutId loadoutId, bool onlyEnabled)
    {
        var entityIds = db.Datoms(
            (ManifestFile, Null.Instance),
            (LoadoutItem.Loadout, loadoutId)
        );

        return entityIds
            .Select(entityId => Load(db, entityId))
            .Where(loadoutItem => loadoutItem.IsValid() && (!onlyEnabled || onlyEnabled && loadoutItem.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().IsEnabled()));
    }
}
