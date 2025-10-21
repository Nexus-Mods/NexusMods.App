using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

[Obsolete("RedMod mod groups should no longer be marked explicitly, they should be identified by the presence of `{Game}/mods/<redModName>/info.json` child file.")]
[Include<LoadoutItemGroup>]
public partial class RedModLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.RedEngine.Cyberpunk2077.RedModLoadoutGroup";
    
    /// <summary>
    /// The info.json file for this RedMod
    /// </summary>
    public static readonly ReferenceAttribute<RedModInfoFile> RedModInfoFile = new(Namespace, nameof(RedModInfoFile));
}


