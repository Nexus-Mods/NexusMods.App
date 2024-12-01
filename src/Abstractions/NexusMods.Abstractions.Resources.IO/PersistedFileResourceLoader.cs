using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Resources.IO;

/// <summary>
/// Loads a persisted file from disk.
/// </summary>
[PublicAPI]
public sealed class PersistedFileResourceLoader<TResourceIdentifier> : IResourceLoader<TResourceIdentifier, byte[]>
    where TResourceIdentifier : notnull
{
    /// <summary>
    /// Converts the resource identifier to a hash.
    /// </summary>
    public delegate Hash IdentifierToHash(TResourceIdentifier resourceIdentifier);

    private readonly AbsolutePath _directory;
    private readonly Extension _extension;
    private readonly IdentifierToHash _identifierToHash;
    private readonly IResourceLoader<TResourceIdentifier, byte[]> _innerLoader;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PersistedFileResourceLoader(
        AbsolutePath directory,
        Extension extension,
        IdentifierToHash identifierToHash,
        IResourceLoader<TResourceIdentifier, byte[]> innerLoader)
    {
        _directory = directory;
        _extension = extension;
        _identifierToHash = identifierToHash;
        _innerLoader = innerLoader;
    }

    /// <inheritdoc/>
    public async ValueTask<Resource<byte[]>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        var hash = _identifierToHash(resourceIdentifier);
        var path = _directory.Combine($"{hash.ToHex()}{_extension.ToString()}");

        var hasFile = path.FileExists;
        await using var stream = path.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        if (hasFile)
        {
            var bytes = GC.AllocateUninitializedArray<byte>(length: (int)stream.Length);
            await stream.ReadExactlyAsync(bytes, cancellationToken);

            return new Resource<byte[]>
            {
                Data = bytes,
            };
        }

        var resource = await _innerLoader.LoadResourceAsync(resourceIdentifier, cancellationToken);
        await stream.WriteAsync(resource.Data, cancellationToken);

        return resource;
    }
}

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class ExtensionMethods
{
    /// <summary>
    /// Persist the resource on disk.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, byte[]> PersistOnDisk<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, byte[]> inner,
        AbsolutePath directory,
        Extension extension,
        PersistedFileResourceLoader<TResourceIdentifier>.IdentifierToHash identifierToHash)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: (directory, extension, identifierToHash),
            factory: static (input, inner) => new PersistedFileResourceLoader<TResourceIdentifier>(
                directory: input.directory,
                extension: input.extension,
                identifierToHash: input.identifierToHash,
                innerLoader: inner
            )
        );
    }
}
