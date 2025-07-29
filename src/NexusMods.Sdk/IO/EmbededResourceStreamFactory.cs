using System.Diagnostics;
using System.Resources;
using NexusMods.Paths;

namespace NexusMods.Sdk.IO;

/// <summary>
/// A Stream Factory that's backed by an embedded resource.
/// </summary>
public class EmbeddedResourceStreamFactory<T> : IStreamFactory
{
    private readonly string _resourceName;

    /// <summary>
    /// Factory for a stream backed by an embedded resource.
    /// </summary>
    public EmbeddedResourceStreamFactory(string resourceName)
    {
        Debug.Assert(RelativePath.FromUnsanitizedInput(resourceName).ToString().Equals(resourceName, StringComparison.Ordinal));
        _resourceName = resourceName;
    }

    /// <inheritdoc/>
    public RelativePath FileName => _resourceName;

    /// <summary>
    /// Returns a stream to the resource.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MissingManifestResourceException"/>
    public ValueTask<Stream> GetStreamAsync()
    {
        var result = typeof(T).Assembly.GetManifestResourceStream(_resourceName);
        if (result == null) throw new MissingManifestResourceException($"Could not find {_resourceName}");
        return ValueTask.FromResult(result);
    }
}
