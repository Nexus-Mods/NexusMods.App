using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Paths;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Download activity for NXM downloads.
/// </summary>
[PublicAPI]
public class NXMDownloadActivity : ADownloadActivity
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public NXMDownloadActivity(
        NXMDownloadState.ReadOnly persistedState,
        INXMDownloader downloader,
        AbsolutePath downloadPath
    ) : base(persistedState.AsPersistedDownloadState(), downloader, downloadPath: downloadPath) { }
}
