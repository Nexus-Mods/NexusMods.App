using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.AdvancedInstaller.Exceptions;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller;

/// <summary>
/// Helpers for throwing methods with high performance.
/// </summary>
internal class ThrowHelpers
{
    public static void MappingAlreadyExists(GamePath outputPath, RelativePath existingPath, RelativePath attemptedPath)
    {
        throw new MappingAlreadyExistsException(outputPath, existingPath, attemptedPath);
    }
}
