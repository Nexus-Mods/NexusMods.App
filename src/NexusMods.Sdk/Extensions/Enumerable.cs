using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/>.
/// </summary>
[PublicAPI]
public static class EnumerableExtensions
{
    /// <summary>
    /// Tries to get the first item matching the <paramref name="predicate"/>.
    /// </summary>
    public static bool TryGetFirst<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate, [NotNullWhen(true)] out T? value)
        where T : notnull
    {
        foreach (var item in enumerable)
        {
            if (!predicate(item)) continue;
            value = item;
            return true;
        }

        value = default(T);
        return false;
    }

    /// <summary>
    /// Tries to get the first item.
    /// </summary>
    public static bool TryGetFirst<T>(this IEnumerable<T> enumerable, [NotNullWhen(true)] out T? value)
        where T : notnull
    {
        foreach (var item in enumerable)
        {
            value = item;
            return true;
        }

        value = default(T);
        return false;
    }

    /// <summary>
    /// Does a linear search and returns the index of the first item that matches the predicate or -1
    /// if no item matches the predicate.
    /// </summary>
    public static int LinearSearch<T>(this IList<T> source, Func<T, bool> predicate)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (predicate(source[i])) return i;
        }

        return -1;
    }
}
