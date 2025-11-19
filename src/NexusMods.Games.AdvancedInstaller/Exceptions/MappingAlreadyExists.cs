using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Games.AdvancedInstaller.Exceptions;

/// <summary>
/// This is an exception thrown when trying to map an file to a location which is already mapped by another file.
/// </summary>
public class MappingAlreadyExistsException : Exception
{
    /// <summary>
    /// Path where the file is to be stored in one of the game directories.
    /// </summary>
    public GamePath OutputPath { get; }

    /// <summary>
    /// The path that is currently mapped.
    /// </summary>
    public RelativePath ExistingPath { get; }

    /// <summary>
    /// The path the user attempted to map with.
    /// </summary>
    public RelativePath AttemptedPath { get; }

    public MappingAlreadyExistsException(GamePath outputPath, RelativePath existingPath, RelativePath attemptedPath)
    {
        OutputPath = outputPath;
        ExistingPath = existingPath;
        AttemptedPath = attemptedPath;
    }
}
