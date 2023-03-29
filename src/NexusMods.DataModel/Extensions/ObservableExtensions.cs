using System.Reactive.Linq;

namespace NexusMods.DataModel.Extensions;

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
