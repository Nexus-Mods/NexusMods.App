using JetBrains.Annotations;
using NexusMods.Abstractions.GC.DataModel;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A variant of <see cref="BackedUpFile"/> to represent a file which has been backed up
/// by the App's Synchronizer.
///
/// This is used when the Synchronizer needs to take a backup of an existing file in the
/// game directory. For example, the replacement of a game file.
/// </summary>
[PublicAPI]
[Include<BackedUpFile>]
public partial class SynchronizerBackedUpFile : IModelDefinition
{
    private const string Namespace = "NexusMods.GC.SynchronizerBackedUpFile";

    /// <summary>
    /// The installation to which this file belongs.
    /// This is stored in the case of a future migration towards a system where
    /// we properly clean up files aft
    /// </summary>
    public static readonly ReferenceAttribute<GameInstallMetadata> GameInstall = new(Namespace, nameof(GameInstall));
}
