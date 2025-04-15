using JetBrains.Annotations;
using NexusMods.Abstractions.GC.DataModel;
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
public class SynchronizerBackedUpFile
{
    
}
