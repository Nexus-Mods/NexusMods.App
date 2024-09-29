using JetBrains.Annotations;

namespace NexusMods.Abstractions.Resources;

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
}
