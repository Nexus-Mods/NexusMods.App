using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents the work associated with downloading a single file.
/// </summary>
[PublicAPI]
public interface IDownloadJob : IJobDefinition<AbsolutePath>
{
    /// <summary>
    /// Gets the destination path for the downloaded file.
    /// </summary>
    AbsolutePath Destination { get; }

    /// <summary>
    /// Adds metadata from the download to the library file
    /// </summary>
    ValueTask AddMetadata(ITransaction transaction, LibraryFile.New libraryFile);
}
