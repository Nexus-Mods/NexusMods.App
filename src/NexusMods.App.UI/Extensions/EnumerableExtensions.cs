using System.Collections.ObjectModel;

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

}
