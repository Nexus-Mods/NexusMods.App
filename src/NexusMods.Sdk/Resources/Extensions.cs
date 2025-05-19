using JetBrains.Annotations;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static partial class Extensions
{
    /// <summary>
    /// Chain loaders.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> Then<TResourceIdentifier, TData, TInnerResourceIdentifier, TInnerData>(
        this IResourceLoader<TInnerResourceIdentifier, TInnerData> innerLoader,
        Func<IResourceLoader<TInnerResourceIdentifier, TInnerData>, IResourceLoader<TResourceIdentifier, TData>> factory)
        where TResourceIdentifier : notnull
        where TInnerResourceIdentifier : notnull
        where TData : notnull
        where TInnerData : notnull
    {
        return factory(innerLoader);
    }

    /// <summary>
    /// Chain loaders.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> Then<TResourceIdentifier, TData, TInnerResourceIdentifier, TInnerData, TState>(
        this IResourceLoader<TInnerResourceIdentifier, TInnerData> innerLoader,
        TState state,
        Func<TState, IResourceLoader<TInnerResourceIdentifier, TInnerData>, IResourceLoader<TResourceIdentifier, TData>> factory)
        where TResourceIdentifier : notnull
        where TInnerResourceIdentifier : notnull
        where TData : notnull
        where TInnerData : notnull
    {
        return factory(state, innerLoader);
    }

    
    
    /// <summary>
    /// Define an anonymous loader that executes a function on the result of the inner loader.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> ThenDo<TResourceIdentifier, TData, TInnerData, TState>(
        this IResourceLoader<TResourceIdentifier, TInnerData> innerLoader,
        TState state,
        Func<TState, TResourceIdentifier, Resource<TInnerData>, CancellationToken, ValueTask<Resource<TData>>> func)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TInnerData : notnull
    {
        return ResourceLoader.Create<TResourceIdentifier, TData, ValueTuple<
            IResourceLoader<TResourceIdentifier, TInnerData>,
            TState,
            Func<TState, TResourceIdentifier, Resource<TInnerData>, CancellationToken, ValueTask<Resource<TData>>>
        >>
        ((innerLoader, state, func), static async (outerState, resourceIdentifier, cancellationToken) =>
        {
            var (innerLoader, innerState, func) = outerState;

            var resource = await innerLoader.LoadResourceAsync(resourceIdentifier, cancellationToken);
            return await func(innerState, resourceIdentifier, resource, cancellationToken);
        });
    }
    
}
