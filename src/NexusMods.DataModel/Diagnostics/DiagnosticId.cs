using System.Globalization;
using JetBrains.Annotations;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// Unique identifier of the diagnostic.
/// </summary>
[PublicAPI]
public readonly struct DiagnosticId : IEquatable<DiagnosticId>, IComparable<DiagnosticId>
{
    /// <summary>
    /// Source of the diagnostic.
    /// </summary>
    /// <remarks>
    /// This should be the name of the assembly that produces the diagnostic.
    /// </remarks>
    /// <example><c>NexusMods.Games.StardewValley</c></example>
    public readonly string Source;

    /// <summary>
    /// Unique monotonic increasing number of the specific diagnostic.
    /// </summary>
    public readonly ushort Number;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DiagnosticId(string source, ushort number)
    {
        Source = source;
        Number = number;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Source}: {Number.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <inheritdoc/>
    public bool Equals(DiagnosticId other)
    {
        if (!string.Equals(Source, other.Source, StringComparison.Ordinal)) return false;
        if (!Number.Equals(other.Number)) return false;
        return true;
    }

    /// <inheritdoc/>
    public int CompareTo(DiagnosticId other)
    {
        var sourceComparison = string.CompareOrdinal(Source, other.Source);
        if (sourceComparison != 0) return sourceComparison;
        return Number.CompareTo(other.Number);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is DiagnosticId other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Number);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(DiagnosticId left, DiagnosticId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(DiagnosticId left, DiagnosticId right)
    {
        return !(left == right);
    }
}
