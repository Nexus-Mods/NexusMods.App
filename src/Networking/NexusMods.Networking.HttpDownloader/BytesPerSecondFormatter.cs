using NexusMods.Abstractions.Jobs;

namespace NexusMods.Networking.HttpDownloader;

/// <summary>
/// Super simple formatter for progress rates.
/// </summary>
public class BytesPerSecondFormatter : IProgressRateFormatter
{
    /// <inheritdoc/>
    public string Format(double value)
    {
        if (value < 1024)
        {
            return $"{value:F2} B/s";
        }

        value /= 1024;
        if (value < 1024)
        {
            return $"{value:F2} KB/s";
        }

        value /= 1024;
        if (value < 1024)
        {
            return $"{value:F2} MB/s";
        }

        value /= 1024;
        return $"{value:F2} GB/s";
    }
}
