using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Downloads;

/// <summary>
/// Represents the work associated with downloading a single file.
/// </summary>
[PublicAPI]
public interface IDownloadJob : IPersistedJob
{
    /// <summary>
    /// Gets the path where the file will be downloaded to.
    /// </summary>
    AbsolutePath DownloadPath { get; }

    void AddMetadata(ITransaction transaction, LibraryFile.New libraryFile);
}
