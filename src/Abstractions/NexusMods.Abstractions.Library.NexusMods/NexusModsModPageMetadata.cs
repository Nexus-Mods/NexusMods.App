using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.NexusModsLibrary;

/// <summary>
/// Represents a remote mod page on NexusMods.
/// </summary>
[PublicAPI]
public partial class NexusModsModPageMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NexusModsModPageMetadata";

    /// <summary>
    /// The ID of the mod page.
    /// </summary>
    public static readonly ModIdAttribute ModId = new(Namespace, nameof(ModId)) { IsIndexed = true };

    /// <summary>
    /// The name of the mod page.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The game of the mod page.
    /// </summary>
    public static readonly GameDomainAttribute Game = new(Namespace, nameof(Game)) { IsIndexed = true };

    /// <summary>
    /// Back-reference to all files from this page.
    /// </summary>
    public static readonly BackReferenceAttribute<NexusModsFileMetadata> Files = new(NexusModsFileMetadata.ModPage);
}
