using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public readonly struct Percent : IEquatable<Percent>, IComparable<Percent>
{
    public readonly double Value;

    public static readonly Percent One = new(1.0);
    public static readonly Percent Zero = new(0.0);

    public Percent(double value)
    {
        ThrowIfInvalid(value);
        Value = value;
    }

    private static void ThrowIfInvalid(double value)
    {
        if (double.IsNaN(value)) throw new ArgumentOutOfRangeException(nameof(value), "Value can't be NaN!");
        if (double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Value can't be infinity!");
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0, nameof(value));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0, nameof(value));
    }

    public static Percent CreateClamped(double value)
    {
        if (double.IsNaN(value)) return Zero;
        if (double.IsInfinity(value)) return One;

        return value switch
        {
            < 0.0 => Zero,
            > 1.0 => One,
            _ => new Percent(value),
        };
    }

    public static Percent Create<TNumber>(TNumber current, TNumber maximum)
    where TNumber : INumber<TNumber>, IConvertible
    {
        var value = current.ToDouble(NumberFormatInfo.InvariantInfo) / maximum.ToDouble(NumberFormatInfo.InvariantInfo);
        return CreateClamped(value);
    }

    /// <inheritdoc/>
    public override string ToString() => ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// To string with a custom format provider
    /// </summary>
    /// <param name="formatProvider"></param>
    /// <returns></returns>
    public string ToString(IFormatProvider formatProvider)
    {
        return Value.ToString("P", formatProvider);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Percent other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(Percent other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public int CompareTo(Percent other) => Value.CompareTo(other.Value);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Percent left, Percent right) => left.Equals(right);
    public static bool operator !=(Percent left, Percent right) => !(left == right);
}
