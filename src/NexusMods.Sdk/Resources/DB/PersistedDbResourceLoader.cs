using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Loads persisted resources from the database.
/// </summary>
[PublicAPI]
public sealed class PersistedDbResourceLoader<TResourceIdentifier> : IResourceLoader<TResourceIdentifier, byte[]>
    where TResourceIdentifier : notnull
{
    /// <summary>
    /// Converts the identifier to a hash.
    /// </summary>
    public delegate Hash IdentifierToHash(TResourceIdentifier resourceIdentifier);

    /// <summary>
    /// Converts the identifier into an entity id.
    /// </summary>
    public delegate EntityId IdentifierToEntityId(TResourceIdentifier resourceIdentifier);

    private readonly IConnection _connection;
    private readonly IResourceLoader<TResourceIdentifier, byte[]> _innerLoader;
    private readonly ReferenceAttribute<PersistedDbResource> _referenceAttribute;
    private readonly IdentifierToHash _identifierToHash;
    private readonly IdentifierToEntityId _identifierToEntityId;
    private readonly AttributeId _referenceAttributeId;
    private readonly Optional<PartitionId> _partitionId;
    private readonly Optional<TimeSpan> _expiresAfter;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PersistedDbResourceLoader(
        IConnection connection,
        ReferenceAttribute<PersistedDbResource> referenceAttribute,
        IdentifierToHash identifierToHash,
        IdentifierToEntityId identifierToEntityId,
        Optional<PartitionId> partitionId,
        Optional<TimeSpan> expiresAfter,
        IResourceLoader<TResourceIdentifier, byte[]> innerLoader)
    {
        _connection = connection;
        _innerLoader = innerLoader;

        _identifierToHash = identifierToHash;
        _identifierToEntityId = identifierToEntityId;

        _referenceAttribute = referenceAttribute;
        _referenceAttributeId = _connection.AttributeCache.GetAttributeId(_referenceAttribute.Id);
        _partitionId = partitionId;
        _expiresAfter = expiresAfter;
    }

    /// <inheritdoc/>
    public ValueTask<Resource<byte[]>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        var entityId = _identifierToEntityId(resourceIdentifier);
        var tuple = (entityId, resourceIdentifier);

        var resource = LoadResource(tuple);
        if (resource is not null) return ValueTask.FromResult(resource);
        return SaveResource(tuple, cancellationToken);
    }

    private Resource<byte[]>? LoadResource(ValueTuple<EntityId, TResourceIdentifier> resourceIdentifier)
    {
        var db = _connection.Db;
        var (entityId, innerResourceIdentifier) = resourceIdentifier;

        var indexSegment = db.Datoms(entityId);
        if (!_referenceAttribute.TryGetValue(indexSegment, out var persistedResourceId))
            return null;
        
        var persistedResource = PersistedDbResource.Load(db, persistedResourceId);

        if (!persistedResource.IsValid()) return null;
        if (persistedResource.IsExpired) return null;

        var hash = _identifierToHash(innerResourceIdentifier);
        if (!persistedResource.ResourceIdentifierHash.Equals(hash)) return null;

        var bytes = persistedResource.Data;

        return new Resource<byte[]>
        {
            Data = bytes.ToArray(),
            ExpiresAt = persistedResource.ExpiresAt.DateTime,
        };
    }

    private async ValueTask<Resource<byte[]>> SaveResource(ValueTuple<EntityId, TResourceIdentifier> resourceIdentifier, CancellationToken cancellationToken)
    {
        var resource = await _innerLoader.LoadResourceAsync(resourceIdentifier.Item2, cancellationToken);

        using var tx = _connection.BeginTransaction();
        var tmpId = _partitionId.HasValue ? tx.TempId(_partitionId.Value) : tx.TempId();

        var expiresAt = _expiresAfter.HasValue ? TimeProvider.System.GetUtcNow() + _expiresAfter.Value : resource.ExpiresAt;
        var persisted = new PersistedDbResource.New(tx, tmpId)
        {
            Data = resource.Data,
            ExpiresAt = expiresAt,
            ResourceIdentifierHash = _identifierToHash(resourceIdentifier.Item2),
        };

        _referenceAttribute.Add(tx, resourceIdentifier.Item1, persisted);
        await tx.Commit();

        return resource;
    }
}
