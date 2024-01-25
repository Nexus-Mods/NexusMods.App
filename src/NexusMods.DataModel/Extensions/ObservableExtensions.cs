using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extension methods for <see cref="IObservable{T}"/>.
/// </summary>
public static class ObservableExtensions
{
    /// <summary>
    /// Returns only values that are not null.
    /// Converts the nullability.
    /// </summary>
    /// <typeparam name="T">The type of value emitted by the observable.</typeparam>
    /// <param name="observable">The observable that can contain nulls.</param>
    /// <returns>A non nullable version of the observable that only emits valid values.</returns>
    public static IObservable<T> NotNull<T>(this IObservable<T?> observable) =>
        observable
            .Where(x => x is not null)
            .Select(x => x!);
}
