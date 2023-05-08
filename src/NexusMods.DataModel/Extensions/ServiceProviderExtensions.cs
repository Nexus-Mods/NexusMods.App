using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    
    /// <summary>
    /// Get all services of type <typeparamref name="T"/> and return them as an <see cref="IEnumerable{T}"/>,
    /// but wrapped in a <see cref="Lazy{T}"/>. This is useful for services that may otherwise create
    /// recursive dependencies.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Lazy<IEnumerable<T>> GetServicesLazily<T>(this IServiceProvider serviceProvider, Func<T, bool>? filterFn)
    {
        filterFn ??= _ => true;
        return new Lazy<IEnumerable<T>>(() => serviceProvider.GetServices<T>().Where(filterFn));
    }
    
    public static Lazy<ILookup<TK, TV>> GetIndexedServicesLazily<TV, TK>(this IServiceProvider serviceProvider, Func<TV, IEnumerable<TK>> keySelector)
    {
        return new Lazy<ILookup<TK, TV>>(() => serviceProvider.GetServices<TV>()
            .SelectMany(x => keySelector(x).Select(y => (key: y, value: x)))
            .ToLookup(kv => kv.key, kv => kv.value));
    }

}
