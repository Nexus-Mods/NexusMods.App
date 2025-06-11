using JetBrains.Annotations;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <summary>
/// Represents a remote file on Nexus Mods which is currently ignored from update checks.
/// </summary>
[PublicAPI]
public partial class IgnoreFileUpdate : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusWebApi.Filters.IgnoreFileUpdateModel";

    /// <summary>
    /// Unique identifier for the file on Nexus Mods to be ignored.
    /// </summary>
    public static readonly UidForFileAttribute Uid = new(Namespace, nameof(Uid)) { IsIndexed = true };
    
    // MaybeTODO: Add the 'previous file' should user feedback say I want to ignore updates from specific version pairs.
}
