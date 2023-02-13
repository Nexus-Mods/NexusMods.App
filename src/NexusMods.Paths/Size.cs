using System.Numerics;
using NexusMods.Paths.Extensions;
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
public readonly partial struct Size : 
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
    /// <summary>
    /// A size that represents 'zero'.
    /// </summary>
    public static readonly Size Zero = From(0);
    
    /// <summary>
    /// A size that represents 'one'.
    /// </summary>
    public static readonly Size One = From(1);

    /// <inheritdoc />
    public static Size MultiplicativeIdentity => One;

    /// <inheritdoc />
    public static Size AdditiveIdentity => Zero;
    
    /// <summary>
    /// Converts a long to a Size object.
    /// </summary>
    public static Size From(long value) => From((ulong)value);
    
    /// <summary>
    /// Represents a size of 1 KiB. (1024 bytes)
    /// </summary>
    public static Size KB => From(1024);
    
    /// <summary>
    /// Represents a size of 1 MiB. (1024^2 bytes)
    /// </summary>
    public static Size MB => From(1024 * 1024);
    
    /// <summary>
    /// Represents a size of 1 GiB. (1024^3 bytes)
    /// </summary>
    public static Size GB => From(1024 * 1024 * 1024);
    
    /// <summary>
    /// Represents a size of 1 TiB. (1024^4 bytes)
    /// </summary>
    public static Size TB => From(1024L * 1024 * 1024 * 1024);

    /// <inheritdoc />
    public static Size operator /(Size left, double right) => From((ulong)(left._value / right));

    /// <inheritdoc />
    public static Size operator *(Size left, double right) => From((ulong)(left._value * right));

    /// <inheritdoc />
    public override string ToString() => _value.ToFileSizeString();

    /// <inheritdoc />
    public static Bandwidth operator /(Size left, TimeSpan right)
    {
        return Bandwidth.From((ulong)(left._value / right.TotalSeconds));
    }
    
    /// <inheritdoc />
    public static Size operator +(Size left, Size right) => From(left._value + right._value);

    /// <inheritdoc />
    public static Size operator -(Size left, Size right) => From(left._value - right._value);

    /// <inheritdoc />
    public static double operator /(Size left, Size right) => (double)left._value / right._value;

    /// <inheritdoc />
    public static bool operator >(Size left, Size right) => left._value > right._value;

    /// <inheritdoc />
    public static bool operator >=(Size left, Size right) => left._value >= right._value;

    /// <inheritdoc />
    public static bool operator <(Size left, Size right) => left._value < right._value;

    /// <inheritdoc />
    public static bool operator <=(Size left, Size right) => left._value <= right._value;
}

public static class SizeExtensions
{
    public static Size Sum<T>(this IEnumerable<T> coll, Func<T, Size> selector)
    {
        return coll.Aggregate(Size.Zero, (s, itm) => selector(itm) + s);
    }

}