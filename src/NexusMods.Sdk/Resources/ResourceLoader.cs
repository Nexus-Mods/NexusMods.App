using JetBrains.Annotations;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Factory methods.
/// </summary>
[PublicAPI]
public static class ResourceLoader
{
    /// <summary>
    /// Create a resource loader.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> Create<TResourceIdentifier, TData>(
        Func<TResourceIdentifier, CancellationToken, ValueTask<Resource<TData>>> func)
        where TResourceIdentifier : notnull
        where TData : notnull
    {
        return new Impl<TResourceIdentifier, TData>(func);
    }

    /// <summary>
    /// Create a resource loader.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> Create<TResourceIdentifier, TData, TState>(
        TState state,
        Func<TState, TResourceIdentifier, CancellationToken, ValueTask<Resource<TData>>> func)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TState : notnull
    {
        return new Impl<TResourceIdentifier, TData, TState>(func, state);
    }

    private sealed class Impl<TResourceIdentifier, TData> : IResourceLoader<TResourceIdentifier, TData>
        where TResourceIdentifier : notnull
        where TData : notnull
    {
        private readonly Func<TResourceIdentifier, CancellationToken, ValueTask<Resource<TData>>> _func;

        public Impl(Func<TResourceIdentifier, CancellationToken, ValueTask<Resource<TData>>> func)
        {
            _func = func;
        }

        public ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
        {
            return _func(resourceIdentifier, cancellationToken);
        }
    }

    private sealed class Impl<TResourceIdentifier, TData, TState> : IResourceLoader<TResourceIdentifier, TData>
        where TResourceIdentifier : notnull
        where TData : notnull
        where TState : notnull
    {
        private readonly Func<TState, TResourceIdentifier, CancellationToken, ValueTask<Resource<TData>>> _func;
        private readonly TState _state;

        public Impl(Func<TState, TResourceIdentifier, CancellationToken, ValueTask<Resource<TData>>> func, TState state)
        {
            _func = func;
            _state = state;
        }

        public ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
        {
            return _func(_state, resourceIdentifier, cancellationToken);
        }
    }
}
