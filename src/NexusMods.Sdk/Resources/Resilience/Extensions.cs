using JetBrains.Annotations;
using Polly;
using Polly.Fallback;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Extension methods.
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// Adds resilience using Polly.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> AddResilience<TResourceIdentifier, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        ResiliencePipeline<Resource<TData>> resiliencePipeline)
        where TResourceIdentifier : notnull
        where TData : notnull
    {
        return inner.Then(
            state: resiliencePipeline,
            factory: static (input, inner) => new PollyWrapper<TResourceIdentifier, TData>(
                resiliencePipeline: input,
                innerLoader: inner
            )
        );
    }

    /// <summary>
    /// Use a fallback value.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, TData> UseFallbackValue<TResourceIdentifier, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        TData fallbackValue)
        where TResourceIdentifier : notnull
        where TData : notnull
    {
        var resiliencePipeline = new ResiliencePipelineBuilder<Resource<TData>>()
            .AddFallback(new FallbackStrategyOptions<Resource<TData>>
            {
                FallbackAction = _ => Outcome.FromResultAsValueTask(new Resource<TData>
                {
                    Data = fallbackValue,
                }),
            })
            .Build();

        return inner.AddResilience(resiliencePipeline);
    }
}
