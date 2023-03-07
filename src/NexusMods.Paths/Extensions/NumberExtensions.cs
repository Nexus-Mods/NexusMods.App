namespace NexusMods.Paths.Extensions;

/// <summary>
/// Various extension methods tied to numbers.
/// </summary>
public static class NumberExtensions
{
    /// <summary>
    /// Returns the human-readable file size for an arbitrary, 64-bit file size <br/>
    /// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB".  
    /// </summary>
    /// <returns></returns>
    /// <remarks>From: https://www.somacon.com/p576.php</remarks>
    public static string ToFileSizeString(this ulong value, string suffix = "")
    {
        // Determine the suffix and readable value
        string units;
        double readable;
        switch (value)
        {
            // Exabyte
            case >= 0x1000000000000000:
                units = "EB";
                readable = value >> 50;
                break;
            // Petabyte
            case >= 0x4000000000000:
                units = "PB";
                readable = value >> 40;
                break;
            // Terabyte
            case >= 0x10000000000:
                units = "TB";
                readable = value >> 30;
                break;
            // Gigabyte
            case >= 0x40000000:
                units = "GB";
                readable = value >> 20;
                break;
            // Megabyte
            case >= 0x100000:
                units = "MB";
                readable = value >> 10;
                break;
            // Kilobyte
            case >= 0x400:
                units = "KB";
                readable = value;
                break;
            default:
                return value.ToString("0 B"); // Byte
        }

        // Divide by 1024 to get fractional value and return formatted number with suffix
        readable /= 1024;
        var formatted = readable.ToString("0.### ") + units;
        if (suffix.Length > 0)
            formatted += suffix;

        return formatted;
    }
}
