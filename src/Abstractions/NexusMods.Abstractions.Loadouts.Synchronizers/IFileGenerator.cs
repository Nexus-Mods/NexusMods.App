using System.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A interface for a file generator. This is used to generate the actual contents of a file during
/// the application of a loadout.
/// </summary>
public interface IFileGenerator : IGuidClass
{
    /// <summary>
    /// Not supported, as this is an interface
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    static UInt128 IGuidClass.Guid => throw new UnreachableException();

    /// <summary>
    /// Writes the contents of the file to the stream.
    /// </summary>
    public ValueTask<Hash?> Write(GeneratedFile.Model generatedFile, Stream stream, Loadout.Model loadout, FlattenedLoadout flattenedLoadout, FileTree fileTree);
}
