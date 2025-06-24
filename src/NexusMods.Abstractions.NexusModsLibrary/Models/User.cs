using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Resources;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// A user on Nexus Mods.
/// </summary>
public partial class User : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.User";
    
    /// <summary>
    /// The user's username.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// The nexus id of the user.
    /// </summary>
    public static readonly UInt64Attribute NexusId = new(Namespace, nameof(NexusId)) { IsIndexed = true };

    /// <summary>
    /// Url to the avatar.
    /// </summary>
    public static readonly UriAttribute AvatarUri = new(Namespace, nameof(AvatarUri));

    /// <summary>
    /// Avatar resource.
    /// </summary>
    public static readonly ReferenceAttribute<PersistedDbResource> AvatarResource = new(Namespace, nameof(AvatarResource)) { IsOptional = true };
}
