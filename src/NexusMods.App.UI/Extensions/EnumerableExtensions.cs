using System.Collections.ObjectModel;
using DynamicData.Kernel;

namespace NexusMods.App.UI.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.Where(x => x is not null).Select(x => x!);
    }

    /// <summary>
    /// Creates a new <see cref="ObservableCollection{T}"/> from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ObservableCollection<T>(source);
    }

    /// <summary>
    /// Creates a new <see cref="ReadOnlyObservableCollection{T}"/> from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ReadOnlyObservableCollection<T> ToReadOnlyObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ReadOnlyObservableCollection<T>(source.ToObservableCollection());
    }

    public static Optional<TItem> OptionalMaxBy<TItem, TValue>(this IEnumerable<TItem> source, Func<TItem, TValue> selector)
        where TItem : notnull
        where TValue : IComparable<TValue>
    {
        return OptionalMaxBy(source, selector, Comparer<TValue>.Default);
    }

    public static Optional<TItem> OptionalMaxBy<TItem, TValue>(this IEnumerable<TItem> source, Func<TItem, TValue> selector, IComparer<TValue> comparer)
        where TItem : notnull
        where TValue : notnull
    {
        var maxItem = Optional<TItem>.None;
        var maxValue = Optional<TValue>.None;

        foreach (var item in source)
        {
            if (!maxItem.HasValue)
            {
                maxItem = item;
                maxValue = selector(item);
                continue;
            }

            var value = selector(item);
            var result = comparer.Compare(value, maxValue.Value);

            // Greater than zero: value comes after maxValue
            if (result > 0)
            {
                maxItem = item;
                maxValue = value;
            }
        }

        return maxItem;
    }

    public static Optional<TItem> OptionalMinBy<TItem, TValue>(this IEnumerable<TItem> source, Func<TItem, TValue> selector)
        where TItem : notnull
        where TValue : IComparable<TValue>
    {
        var minItem = Optional<TItem>.None;
        var minValue = Optional<TValue>.None;

        foreach (var item in source)
        {
            if (!minItem.HasValue)
            {
                minItem = item;
                minValue = selector(item);
                continue;
            }

            var value = selector(item);
            var result = value.CompareTo(minValue.Value);

            // Smaller than zero: value comes before minValue
            if (result < 0)
            {
                minItem = item;
                minValue = value;
            }
        }

        return minItem;
    }
}
