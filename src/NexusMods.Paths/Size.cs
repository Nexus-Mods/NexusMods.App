using System.Numerics;
using Vogen;
// ReSharper disable InconsistentNaming

namespace NexusMods.Paths;

/// <summary>
/// There are many cases where a value in the app should be a positive number attached to the size of some data,
/// instead of leaving this data unmarked, we wrap it in a readonly value struct to make it explicit.
///
/// Several arithmetic operators only make sense when one side of the operation has undefined units. For example,
/// 1MB * 1MB is technically 1MB^2, but we don't want to allow that because it's not a valid size.
/// </summary>
[ValueObject<ulong>]
public partial struct Size : 
    IAdditionOperators<Size, Size, Size>, 
    ISubtractionOperators<Size, Size, Size>,
    IDivisionOperators<Size, Size, double>,
    IDivisionOperators<Size, double, Size>,
    IDivisionOperators<Size, TimeSpan, Bandwidth>,
    IMultiplyOperators<Size, double, Size>,
    IComparisonOperators<Size, Size, bool>,
    IMultiplicativeIdentity<Size, Size>,
    IAdditiveIdentity<Size, Size>
{
    public static readonly Size Zero = From(0);
    public static readonly Size One = From(1);
    
    public static Size From(long value) => From((ulong)value);

    
    public static Size operator +(Size left, Size right)
    {
        return From(left._value + right._value);
    }
    public static Size operator -(Size left, Size right)
    {
        return From(left._value - right._value);
    }

    public static double operator /(Size left, Size right)
    {
        return (double)left._value / right._value;
    }

    public static bool operator >(Size left, Size right)
    {
        return left._value > right._value;
    }

    public static bool operator >=(Size left, Size right)
    {
        return left._value >= right._value;
    }

    public static bool operator <(Size left, Size right)
    {
        return left._value < right._value;
    }
    
    public static bool operator <=(Size left, Size right)
    {
        return left._value <= right._value;
    }

    public static Size MultiplicativeIdentity => One;
    public static Size AdditiveIdentity => Zero;
    
    public static Size KB => From(1024);
    public static Size MB => From(1024 * 1024);
    public static Size GB => From(1024 * 1024 * 1024);
    public static Size TB => From(1024L * 1024 * 1024 * 1024);
    public static Size operator /(Size left, double right)
    {
        return From((ulong)(left._value / right));
    }

    public static Size operator *(Size left, double right)
    {
        return From((ulong)(left._value * right));
    }
    
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
                return _value.ToString("0 B"); // Byte
        }
        // Divide by 1024 to get fractional value
        readable /= 1024;
        // Return formatted number with suffix
        return readable.ToString("0.### ") + suffix;
    }
    public override string ToString()
    {
        return Readable();
    }

    public static Bandwidth operator /(Size left, TimeSpan right)
    {
        return Bandwidth.From((ulong)(left._value / right.TotalSeconds));
    }
}