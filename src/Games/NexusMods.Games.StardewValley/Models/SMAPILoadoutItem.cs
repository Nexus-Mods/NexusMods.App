using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.StardewValley.Models;

[PublicAPI]
[Include<LoadoutItemGroup>]
public partial class SMAPILoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.StardewValley.SMAPILoadoutItem";

    /// <summary>
    /// The version of SMAPI.
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version)) { IsOptional = true };

    /// <summary>
    /// Reference to the mod database loadout file.
    /// </summary>
    public static readonly ReferenceAttribute<SMAPIModDatabaseLoadoutFile> ModDatabase = new(Namespace, nameof(ModDatabase)) { IsOptional = true};
}
