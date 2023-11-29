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
    /// Returns the value if the optional has a value, otherwise calls <see cref="alternativeValueFunc"/> and returns
    /// the return value.
    /// </summary>
    public static T ValueOr<T>(this Optional<T> optional, Func<T> alternativeValueFunc) where T : class
    {
        return optional.HasValue ? optional.Value : alternativeValueFunc();
    }
}
