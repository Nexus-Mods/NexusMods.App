using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a group of game file loadout items.
/// </summary>
[PublicAPI]
[Include<LoadoutItemGroup>]
public partial class LoadoutGameFilesGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutGameFilesGroup";

    /// <summary>
    /// Game metadata.
    /// </summary>
    public static readonly ReferenceAttribute<Sdk.Games.GameInstallMetadata> GameMetadata = new(Namespace, nameof(GameMetadata));
}


