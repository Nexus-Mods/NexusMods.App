using JetBrains.Annotations;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

[PublicAPI]
[Include<SortableItemModel>]
public partial class RedModSortableItemModel : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.RedEngine.Cyberpunk2077.RedModSortableItemModel";
    
    /// <summary>
    /// The identifier used by the game for the RedMod load order
    /// </summary>
    public static readonly StringAttribute RedModFolderName = new(Namespace, nameof(RedModFolderName));
    
}
