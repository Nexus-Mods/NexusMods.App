using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.StardewValley.Models;

[Include<LoadoutFile>]
public partial class SMAPIModDatabaseLoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.StardewValley.SMAPIModDatabaseLoadoutFile";

    public static readonly MarkerAttribute ModDatabaseFile = new(Namespace, nameof(ModDatabaseFile));
}
