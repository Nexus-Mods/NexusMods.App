using Vogen;

namespace NexusMods.Paths;

/// <summary>
/// Represents bandwidth in bytes per second.
/// </summary>
[ValueObject<ulong>]
public partial struct Bandwidth
{
    
    // From : https://www.somacon.com/p576.php
    // Returns the human-readable file size for an arbitrary, 64-bit file size 
    // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
    public string Readable()
    {
        // Determine the suffix and readable value
        string suffix;
        double readable;
        switch (_value)
        {
            // Exabyte
            case >= 0x1000000000000000:
                suffix = "EB";
                readable = _value >> 50;
                break;
            // Petabyte
            case >= 0x4000000000000:
                suffix = "PB";
                readable = _value >> 40;
                break;
            // Terabyte
            case >= 0x10000000000:
                suffix = "TB";
                readable = _value >> 30;
                break;
            // Gigabyte
            case >= 0x40000000:
                suffix = "GB";
                readable = _value >> 20;
                break;
            // Megabyte
            case >= 0x100000:
                suffix = "MB";
                readable = _value >> 10;
                break;
            // Kilobyte
            case >= 0x400:
                suffix = "KB";
                readable = _value;
                break;
            default:
                return _value.ToString("0 B/sec"); // Byte
        }
        // Divide by 1024 to get fractional value
        readable /= 1024;
        // Return formatted number with suffix
        return readable.ToString("0.### ") + suffix + "/sec";
    }
    public override string ToString()
    {
        return Readable();
    }
}