using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// Interface for a file that is generated at apply time, not stored in the archive manager
/// </summary>
public interface IGeneratedFile
{
    /// <summary>
    /// Writes the file to the given stream, returning the hash of the file. If hashing the file
    /// would require a full reading of the stream, return null, and the callee will hash the file.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public ValueTask<Hash?> Write(Stream stream);
}
