using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents the library.
/// </summary>
[PublicAPI]
public interface ILibraryService
{
    /// <summary>
    /// Adds a local file to the library.
    /// </summary>
    IJob AddLocalFile(AbsolutePath absolutePath, CancellationToken cancellationToken = default);
}
