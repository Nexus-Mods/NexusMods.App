using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Settings for <see cref="HttpDownloadJobWorker"/>.
/// </summary>
[PublicAPI]
public record HttpDownloaderSettings : ISettings
{
    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }
}
