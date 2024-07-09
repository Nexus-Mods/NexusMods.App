using System.Globalization;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents the rate at which work is being done.
/// </summary>
[PublicAPI]
public readonly struct ProgressRate
{
    /// <summary>
    /// The raw value.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// The formatter.
    /// </summary>
    public readonly IProgressRateFormatter? Formatter;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProgressRate(double value, IProgressRateFormatter formatter)
    {
        Value = value;
        Formatter = formatter;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var formatted = Formatter?.Format(Value);
        return formatted ?? Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Value.Equals(obj);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(ProgressRate left, ProgressRate right) => left.Equals(right);
    public static bool operator !=(ProgressRate left, ProgressRate right) => !(left == right);
}
