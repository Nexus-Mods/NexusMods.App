using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
namespace NexusMods.Abstractions.GC.DataModel;

/// <summary>
/// Represents a file which is stored as part of a backup.
/// This is a file that forms a GC root.
///
/// Specific types of backed-up files will be derived from this class.
/// </summary>
[PublicAPI]
public partial class BackedUpFile : IModelDefinition
{
    private const string Namespace = "NexusMods.GC.BackedUpFile";

    /// <summary>
    /// Hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { IsIndexed = true };
}
