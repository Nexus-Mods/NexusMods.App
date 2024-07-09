using DynamicData.Kernel;
using JetBrains.Annotations;
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
    Task<Optional<LocalFile.ReadOnly>> AddLocalFileAsync(AbsolutePath absolutePath, CancellationToken cancellationToken = default);
}
