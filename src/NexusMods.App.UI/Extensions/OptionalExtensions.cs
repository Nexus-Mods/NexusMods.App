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
    public static T ValueOr<T>(this Optional<T> optional, T alternativeValue) where T : struct
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
}
