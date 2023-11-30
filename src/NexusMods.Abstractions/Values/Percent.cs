namespace NexusMods.Abstractions.Values;

/// <summary>
/// Represents a percentage; used for reporting progress of various operations
/// throughout.
/// </summary>
public readonly struct Percent : IComparable, IEquatable<Percent>
{
    /// <summary>
    /// A pre-determined value that represents a value of 100 percent.
    /// </summary>
    public static readonly Percent One = new(1d);

    /// <summary>
    /// A pre-determined value that represents a value of 0 percent.
    /// </summary>
    public static readonly Percent Zero = new(0d);

    /// <summary>
    /// The raw percentage value specified in range of 0.0 to 1.0.
    /// </summary>
    public readonly double Value;

    private Percent(double d, bool check)
    {
        // TODO: ThrowHelper this exception throw out. https://github.com/Nexus-Mods/NexusMods.App/issues/214
        if (!check || InRange(d))
            Value = d;
        else
            throw new ArgumentException("Element out of range: " + d);
    }

    /// <summary>
    /// Creates a new percentage
    /// </summary>
    /// <param name="d">A percentage value, this value should be within the range 0.0 to 1.0 (100%)</param>
    public Percent(double d)
        : this(d, true)
    {
    }

    /// <summary>
    /// Inverses the percentage reading, converting it from e.g. 10% done to 90% left.
    /// </summary>
    public Percent Inverse => new(1d - Value, false);

    /// <summary>
    /// Determines if a given value/percentage is within permissible range.
    /// </summary>
    /// <param name="fraction">A percentage expressed in the range between 0.0 and 1.0.</param>
    /// <returns>True if the fraction fits within the range.</returns>
    public static bool InRange(double fraction)
    {
        return fraction is >= 0 and <= 1;
    }

    /// <summary>
    /// Creates a percentage, clamping the value between 0% [0.0] and 100% [100.0]
    /// </summary>
    /// <param name="d">The value being created.</param>
    /// <exception cref="ArgumentException">Not a valid floating point number [NaN or Infinity].</exception>
    public static Percent CreateClamped(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d))
            throw new ArgumentException();

        return d switch
        {
            < 0 => Zero,
            > 1 => One,
            _ => new Percent(d, false)
        };
    }

    /// <summary>
    /// Creates a percentage given a current and maximum value.
    /// The resulting value is clamped between 0% and 100%
    /// </summary>
    /// <param name="cur">Current value.</param>
    /// <param name="max">Maximum value.</param>
    public static Percent CreateClamped(int cur, int max)
    {
        return CreateClamped(1.0d * cur / max);
    }

    /// <summary>
    /// Creates a percentage given a current and maximum value.
    /// The resulting value is clamped between 0% and 100%
    /// </summary>
    /// <param name="cur">Current value.</param>
    /// <param name="max">Maximum value.</param>
    public static Percent CreateClamped(long cur, long max) => CreateClamped(1.0d * cur / max);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Percent rhs)
            return false;

        return Equals(rhs);
    }

    /// <inheritdoc />
    public bool Equals(Percent other)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        // As intended.
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => ToString(0);

    /// <summary>
    /// Formats the current percentage as a string, using a custom format.
    /// </summary>
    /// <param name="format">Numeric format specifier, see <a href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings"></a></param>
    /// <returns>Formatted percentage as a string.</returns>
    public string ToString(string format)
    {
        return $"{(Value * 100).ToString(format)}%";
    }

    /// <summary>
    /// Formats the current percentage as a string, with specified number of decimal places.
    /// </summary>
    /// <param name="numDecimals">Number of decimal places.</param>
    /// <returns>Formatted percentage as a string.</returns>
    public string ToString(int numDecimals)
    {
        return ToString("n" + numDecimals);
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is Percent rhs)
            return Value.CompareTo(rhs.Value);

        return 0;
    }

    /// <summary>
    /// Parses a percentage specified as a string [range 0.0 - 100.0] back into a new percent instance.
    /// </summary>
    /// <param name="str">A string representing the percentage as a number between 0 and 100.</param>
    /// <param name="p">The percentage item returned.</param>
    /// <returns></returns>
    public static bool TryParse(string str, out Percent p)
    {
        // TODO: This will not parse a value like 3.33%, as `TryParse` will not accept % suffix. https://github.com/Nexus-Mods/NexusMods.App/issues/209
        if (double.TryParse(str, out var d))
        {
            d /= 100;
            if (InRange(d))
            {
                p = new Percent(d);
                return true;
            }
        }

        p = default;
        return false;
    }

    #region Operators and Conversions

#pragma warning disable CS1591
    public static Percent operator +(Percent c1, Percent c2) => new(c1.Value + c2.Value);

    public static Percent operator -(Percent c1, Percent c2) => new(c1.Value - c2.Value);

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public static bool operator ==(Percent c1, Percent c2) => c1.Value == c2.Value;

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public static bool operator !=(Percent c1, Percent c2) => c1.Value != c2.Value;

    public static bool operator >(Percent c1, Percent c2) => c1.Value > c2.Value;

    public static bool operator <(Percent c1, Percent c2) => c1.Value < c2.Value;

    public static bool operator >=(Percent c1, Percent c2) => c1.Value >= c2.Value;

    public static bool operator <=(Percent c1, Percent c2) => c1.Value <= c2.Value;

    public static explicit operator double(Percent c1) => c1.Value;
#pragma warning restore CS1591
    #endregion
}
