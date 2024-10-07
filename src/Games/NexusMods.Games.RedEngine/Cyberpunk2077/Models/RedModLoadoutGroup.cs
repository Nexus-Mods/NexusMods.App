using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

[Include<LoadoutItemGroup>]
public partial class RedModLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.RedEngine.Cyberpunk2077.RedModLoadoutGroup";
    
    /// <summary>
    /// The info.json file for this RedMod
    /// </summary>
    public static readonly ReferenceAttribute<RedModInfoFile> RedModInfoFile = new(Namespace, nameof(RedModInfoFile));
    
    /// <summary>
    /// The order in which this RedMod should be loaded relative to other RedMods
    /// </summary>
    public static readonly UInt32Attribute SortIndex = new(Namespace, nameof(SortIndex));
}

