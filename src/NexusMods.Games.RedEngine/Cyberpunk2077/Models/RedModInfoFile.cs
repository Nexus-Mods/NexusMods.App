using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Models;


[Include<LoadoutFile>]
public partial class RedModInfoFile : IModelDefinition
{
    private static string Namespace => "NexusMods.Games.RedEngine.Cyberpunk2077.RedModInfoFile";
    
    /// <summary>
    /// The internal name of the mod
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The internal version of the mod
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));
}
