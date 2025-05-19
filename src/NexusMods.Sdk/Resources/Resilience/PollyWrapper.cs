using JetBrains.Annotations;
using Polly;

namespace NexusMods.Sdk.Resources.Resilience;

/// <summary>
/// Uses a resilience pipeline.
/// </summary>
[PublicAPI]
public sealed class PollyWrapper<TResourceIdentifier, TData> : IResourceLoader<TResourceIdentifier, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
{
    private readonly ResiliencePipeline<Resource<TData>> _resiliencePipeline;
    private readonly IResourceLoader<TResourceIdentifier, TData> _innerLoader;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PollyWrapper(
        ResiliencePipeline<Resource<TData>> resiliencePipeline,
        IResourceLoader<TResourceIdentifier, TData> innerLoader)
    {
        _resiliencePipeline = resiliencePipeline;
        _innerLoader = innerLoader;
    }

    /// <inheritdoc/>
    public ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        return _resiliencePipeline.ExecuteAsync(callback: static (state, cancellationToken) =>
        {
            var (innerLoader, resourceIdentifier) = state;
            return innerLoader.LoadResourceAsync(resourceIdentifier, cancellationToken);
        }, state: (_innerLoader, resourceIdentifier), cancellationToken: cancellationToken);
    }
}
