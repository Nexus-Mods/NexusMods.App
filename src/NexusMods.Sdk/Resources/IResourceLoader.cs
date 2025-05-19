using JetBrains.Annotations;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Represents a resource loader.
/// </summary>
[PublicAPI]
public interface IResourceLoader<in TResourceIdentifier, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
{
    /// <summary>
    /// Loads the resource.
    /// </summary>
    ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken);
}
