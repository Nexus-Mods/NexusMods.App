using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// A user on Nexus Mods.
/// </summary>
public partial class User : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.NexusModsLibrary.User";
    
    /// <summary>
    /// The user's username.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// The nexus id of the user.
    /// </summary>
    public static readonly ULongAttribute NexusId = new(Namespace, nameof(NexusId)) { IsIndexed = true };
    
    /// <summary>
    /// The user's avatar URL.
    /// </summary>
    public static readonly UriAttribute Avatar = new(Namespace, nameof(Avatar));
    
    /// <summary>
    /// The user's avatar image.
    /// </summary>
    public static readonly MemoryAttribute AvatarImage = new(Namespace, nameof(AvatarImage));
}
