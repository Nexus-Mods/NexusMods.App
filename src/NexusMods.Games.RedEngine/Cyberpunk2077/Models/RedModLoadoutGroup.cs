using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;

[Include<LoadoutItemGroup>]
public partial class RedModLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.RedEngine.Cyberpunk2077.RedModLoadoutGroup";
    
    /// <summary>
    /// The info.json file for this RedMod
    /// </summary>
    public static readonly ReferenceAttribute<RedModInfoFile> RedModInfoFile = new(Namespace, nameof(RedModInfoFile));
}

public static class RedModLoadoutItemGroupExtensions 
{
    
    public static bool IsEnabled(this RedModLoadoutGroup.ReadOnly grp)
    {
        return grp.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled();
    }

    public static RelativePath RedModFolder(this RedModLoadoutGroup.ReadOnly group)
    {
        var redModInfoFile = group.RedModInfoFile.AsLoadoutFile().AsLoadoutItemWithTargetPath().TargetPath.Item3;
        return redModInfoFile.Parent.FileName;
    }
}

