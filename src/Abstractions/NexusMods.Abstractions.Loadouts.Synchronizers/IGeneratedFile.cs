using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Interface for a file that is generated at apply time, not stored in the file store
/// </summary>
public interface IGeneratedFile
{
    /// <summary>
    /// Writes the file to the given stream, returning the hash of the file. If hashing the file
    /// would require a full reading of the stream, return null, and the callee will hash the file.
    /// </summary>
    /// <param name="flattenedLoadout"></param>
    /// <param name="fileTree"></param>
    /// <param name="stream"></param>
    /// <param name="loadout"></param>
    /// <returns></returns>
    ValueTask<Hash?> Write(Stream stream, Loadout loadout, FlattenedLoadout flattenedLoadout, FileTree fileTree);

    /// <summary>
    /// Called when the file is updated on disk, outside of the application, this method should read the file,
    /// and update the IGeneratedFile accordingly.
    /// </summary>
    /// <param name="newEntry"></param>
    /// <param name="stream"></param>
    /// <returns></returns>
    ValueTask<AModFile> Update(DiskStateEntry newEntry, Stream stream);
}
