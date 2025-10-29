using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.StardewValley.Models;

[Include<LoadoutFile>]
public partial class SMAPIManifestLoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.StardewValley.SMAPIManifestLoadoutFile";

    public static readonly MarkerAttribute ManifestFile = new(Namespace, nameof(ManifestFile)) { IsIndexed = true };

    public static IEnumerable<ReadOnly> GetAllInLoadout(IDb db, LoadoutId loadoutId, bool onlyEnabled)
    {
        var entityIds = db.Connection.Query<EntityId>($"SELECT Id FROM mdb_SMAPIManifestLoadoutFile(Db=>{db}) WHERE LoadoutId = {loadoutId.Value} AND ManifestFile = true");

        return entityIds
            .Select(entityId => Load(db, entityId))
            .Where(loadoutItem => loadoutItem.IsValid() && (!onlyEnabled || onlyEnabled && loadoutItem.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().IsEnabled()));
    }
}
