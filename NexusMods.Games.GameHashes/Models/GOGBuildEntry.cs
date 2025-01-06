using NexusMods.Games.GameHashes.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.GameHashes.Models;

/// <summary>
/// An entry in a GOG Build
/// </summary>
public partial class GOGBuildEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.GameHashes.GOGBuildEntry";
    
    public static readonly GOGBuildIdAttribute BuildId = new(Namespace, "BuildId") { IsIndexed = true };
    
    public static readonly RelativePathAttribute Path = new(Namespace, "Path") { IsIndexed = true };
    
    public static readonly ReferenceAttribute<HashRelation> Hash = new(Namespace, "HashRelation") { IsIndexed = true };
}
