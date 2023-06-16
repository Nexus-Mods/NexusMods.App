namespace NexusMods.App.UI.Helpers;

/// <summary>
/// Class that helps you format file sizes as strings.
/// </summary>
public static class StringFormatters
{
    /// <summary>
    /// Formats the file size as a string.
    /// </summary>
    /// <param name="currentBytes">The currently downloaded number of bytes.</param>
    /// <param name="totalBytes">The total number of downloaded bytes.</param>
    /// <remarks>Complete number of downloaded bytes.</remarks>
    public static string ToGB(long currentBytes, long totalBytes)
    {
        const float scale = 1000 * 1000 * 1000;
        var usedGB = currentBytes / scale;
        var totalGB = totalBytes / scale;

        return $"{usedGB:F2} GB / {totalGB:F2} GB";
    }

    /// <summary>
    /// Formats the seconds remaining in format '0 mins', '0 secs' etc.
    /// </summary>
    /// <param name="secondsRemaining">Seconds remaining</param>
    /// <remarks>Complete number of downloaded bytes.</remarks>
    public static string ToTimeRemainingShort(int secondsRemaining)
    {
        return secondsRemaining switch
        {
            < 60 => $"{secondsRemaining} secs",
            >= 60 and < 3600 => $"{secondsRemaining / 60} mins",
            >= 3600 => $"{secondsRemaining / 3600} hours"
        };
    }
}
