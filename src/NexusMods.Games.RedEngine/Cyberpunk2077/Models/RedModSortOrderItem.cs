using JetBrains.Annotations;
using NexusMods.Abstractions.Games;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

[PublicAPI]
[Include<SortOrderItem>]
public partial class RedModSortOrderItem : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.RedEngine.Cyberpunk2077.RedModSortableEntry";
    
    /// <summary>
    /// The identifier used by the game for the RedMod load order
    /// </summary>
    public static readonly RelativePathAttribute RedModFolderName = new(Namespace, nameof(RedModFolderName));
}
