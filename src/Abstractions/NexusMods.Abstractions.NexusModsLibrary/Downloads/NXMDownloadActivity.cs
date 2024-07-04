using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;

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
        INXMDownloader downloader
    ) : base(persistedState.AsPersistedDownloadState(), downloader, title: ToTitle(persistedState)) { }

    private static string ToTitle(NXMDownloadState.ReadOnly state)
    {
        var fileMetadata = state.FileMetadata;
        var modPageMetadata = fileMetadata.ModPage;

        return $"{modPageMetadata.Name} - {fileMetadata.Name}";
    }
}
