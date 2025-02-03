using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Extensions;

[PublicAPI]
public static class OptionalExtensions
{
    /// <summary>
    /// Returns the value if the optional has a value, otherwise returns the provided alternative value.
    /// </summary>
    /// <param name="optional">The source.</param>
    /// <param name="alternativeValue">The alternative value.</param>
    /// <typeparam name="T">The Type of the item.</typeparam>
    public static T ValueOr<T>(this Optional<T> optional, T alternativeValue) where T : notnull
    {
        return optional.HasValue ? optional.Value : alternativeValue;
    }

    /// <summary>
    /// Tries to get the value out of the <see cref="Optional{T}"/>.
    /// </summary>
    public static bool TryGet<T>(this Optional<T> optional, [NotNullWhen(true)] out T? value) where T : notnull
    {
        if (optional.HasValue)
        {
            value = optional.Value;
            return true;
        }

        value = default;
        return false;
    }

    public static int Compare<T>(this Optional<T> a, Optional<T> b, Func<T, T, int> comparer)
        where T : notnull
    {
        var (x, y) = (a.HasValue, b.HasValue);
        return (x, y) switch
        {
            // both have values
            (true, true) => comparer(a.Value, b.Value),

            // b precedes a
            (true, false) => 1,

            // a precedes b
            (false, true) => -1,

            // a and b are in the same position
            (false, false) => 0,
        };
    }

    /// <inheritdoc cref="IComparer{T}.Compare"/>
    public static int Compare<T>(this Optional<T> a, Optional<T> b)
        where T : IComparable<T>
    {
        return Compare(a, b, static (a, b) => a.CompareTo(b));
    }
}
