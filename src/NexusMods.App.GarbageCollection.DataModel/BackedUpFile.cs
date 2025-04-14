using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.App.GarbageCollection.DataModel;

/// <summary>
/// Represents a file which is stored as part of a backup.
/// This is a file that forms a GC root.
///
/// Specific types of backed up files will derive from this class.
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
[PublicAPI]
public partial class BackedUpFile : IModelDefinition
{
    private const string Namespace = "NexusMods.GC.BackedUpFile";

    /// <summary>
    /// Hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { IsIndexed = true };
}
