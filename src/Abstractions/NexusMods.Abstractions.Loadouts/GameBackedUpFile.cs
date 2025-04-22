using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// This is a GC root that prevents garbage collection of a file.
/// 
/// This represents a file which belongs to the game, i.e. game files.
/// 
/// This file is backed up by the Synchronizer, when it decides to take a backup
/// of game files which are on disk and not archived. (Needed for rollback/undo)
///
/// Mechanics are roughly as follows:
///
/// -> File in game hashes DB => GameBackedUpFile   | (rooted by GameBackedUpFile via file hashes DB match)
/// -> File in overrides => !GameBackedUpFile       | (rooted by overrides mod)
/// </summary>
/// <remarks>
///     There is a 'catch', for when we don't have game files in the hashes DB, 
///     they may end up in overrides, which always win. Problematic if a mod requires
///     to override base game files. But as discussed in a meeting, that is a problem
///     for another day.
/// </remarks>
[PublicAPI]
public partial class GameBackedUpFile : IModelDefinition
{
    private const string Namespace = "NexusMods.GC.BackedUpFile";

    /// <summary>
    /// Hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));
    
    /// <summary>
    /// The installation to which this file belongs.
    /// This is stored in the case of a future migration towards a system where
    /// we properly clean up files aft
    /// </summary>
    public static readonly ReferenceAttribute<GameInstallMetadata> GameInstall = new(Namespace, nameof(GameInstall));
}
