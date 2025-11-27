using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a file in a Loadout.
/// </summary>
/// <remarks>
///     Note(sewer):
/// 
///     DO NOT ADD ANY OTHER PRIMITIVES THAT CAN STORE FILES IN THE 'ARCHIVES'
///     LOCATIONS DEMARKED BY 'DataModelSettings.ArchiveLocations' WITHOUT
///     UPDATING THE CODE OF THE GARBAGE COLLECTOR.
///
///     FAILURE TO DO SO WILL RESULT IN CATASTROPHIC FAILURE IN THE FORM OF
///     USERS LOSING FILES THAT MAY STILL BE IN USE.
/// </remarks>
[Include<LoadoutItemWithTargetPath>]
[PublicAPI]
public partial class LoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutFile";

    /// <summary>
    /// Hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { IsIndexed = true };

    /// <summary>
    /// Size of the file.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));

    public partial struct ReadOnly : IHavePathHashSizeAndReference
    {

#region IHavePathHashSizeAndReference

        GamePath IHavePathHashSizeAndReference.Path => LoadoutItemWithTargetPath.TargetPath.Get(this);

        Hash IHavePathHashSizeAndReference.Hash => LoadoutFile.Hash.Get(this);

        Size IHavePathHashSizeAndReference.Size => LoadoutFile.Size.Get(this);
        EntityId IHavePathHashSizeAndReference.Reference => Id;

#endregion
    }

}
