using System.Globalization;
using System.Numerics;

namespace NexusMods.Paths;

public readonly struct Size : 
    IEquatable<Size>, 
    IComparable<Size>, 
    IAdditionOperators<Size, Size, Size>, 
    IAdditiveIdentity<Size, Size>,
    IDivisionOperators<Size, Size, double>,
    IEqualityOperators<Size, Size, bool>
{
    private readonly ulong _size = 0;

    private Size(ulong size)
    {
        _size = size;
    }
    
    public static implicit operator ulong(Size s)
    {
        return s._size;
    }
    
    public static implicit operator Size(ulong s)
    {
        return new Size(s);
    }
    
    public static implicit operator long(Size s)
    {
        return (long)s._size;
    }
    
    public static implicit operator Size(long s)
    {
        if (s < 0)
            throw new Exception("Cannot cast negative number to Size");
        return new Size((ulong)s);
    }
    
    public int CompareTo(Size other)
    {
        return _size.CompareTo(other);
    }

    public bool Equals(Size other)
    {
        return _size == other._size;
    }

    public override int GetHashCode()
    {
        return _size.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is Size s) return _size == s._size;
        return false;
    }

    public override string ToString()
    {
        return Readable();
    }

    // From : https://www.somacon.com/p576.php
    // Returns the human-readable file size for an arbitrary, 64-bit file size 
    // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
    public string Readable()
    {
        // Determine the suffix and readable value
        string suffix;
        double readable;
        switch (_size)
        {
            // Exabyte
            case >= 0x1000000000000000:
                suffix = "EB";
                readable = _size >> 50;
                break;
            // Petabyte
            case >= 0x4000000000000:
                suffix = "PB";
                readable = _size >> 40;
                break;
            // Terabyte
            case >= 0x10000000000:
                suffix = "TB";
                readable = _size >> 30;
                break;
            // Gigabyte
            case >= 0x40000000:
                suffix = "GB";
                readable = _size >> 20;
                break;
            // Megabyte
            case >= 0x100000:
                suffix = "MB";
                readable = _size >> 10;
                break;
            // Kilobyte
            case >= 0x400:
                suffix = "KB";
                readable = _size;
                break;
            default:
                return _size.ToString("0 B"); // Byte
        }
        // Divide by 1024 to get fractional value
        readable /= 1024;
        // Return formatted number with suffix
        return readable.ToString("0.### ") + suffix;
    }
    
    public static bool operator ==(Size left, Size right) => left._size == right._size;

    public static bool operator !=(Size left, Size right) => left._size != right._size;

    public static bool operator >(Size left, Size right) => left._size > right._size;

    public static bool operator >=(Size left, Size right) => left._size >= right._size;

    public static bool operator <(Size left, Size right) => left._size < right._size;

    public static bool operator <=(Size left, Size right) => left._size <= right._size;

    public static Size operator /(Size left, Size right) => left._size / right._size;
    
    public static Size MultiplicativeIdentity => One;
    public static Size operator *(Size left, Size right) => left._size * right._size;

    public static Size operator -(Size left, Size right) => left._size - right._size;
    public static Size operator +(Size left, Size right) => left._size + right._size;
    public static Size One => 1L;
    public static Size Zero => 0L;
    public static Size AdditiveIdentity => Zero;
    
    static double IDivisionOperators<Size, Size, double>.operator /(Size left, Size right)
    {
        return (double)left._size / right._size;
    }
}