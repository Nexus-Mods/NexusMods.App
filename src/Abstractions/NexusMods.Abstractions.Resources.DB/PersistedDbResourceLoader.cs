using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.Resources.DB;

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

    /// <summary>
    /// Constructor.
    /// </summary>
    public PersistedDbResourceLoader(
        IConnection connection,
        ReferenceAttribute<PersistedDbResource> referenceAttribute,
        IdentifierToHash identifierToHash,
        IdentifierToEntityId identifierToEntityId,
        Optional<PartitionId> partitionId,
        IResourceLoader<TResourceIdentifier, byte[]> innerLoader)
    {
        _connection = connection;
        _innerLoader = innerLoader;

        _identifierToHash = identifierToHash;
        _identifierToEntityId = identifierToEntityId;

        _referenceAttribute = referenceAttribute;
        _referenceAttributeId = _connection.AttributeCache.GetAttributeId(_referenceAttribute.Id);
        _partitionId = partitionId;
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

        var persistedResourceId = Optional<EntityId>.None;
        var indexSegment = db.Datoms(entityId);
        foreach (var datom in indexSegment)
        {
            if (!datom.A.Equals(_referenceAttributeId)) continue;
            persistedResourceId = _referenceAttribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, _connection.AttributeResolver);
        }

        if (!persistedResourceId.HasValue) return null;
        var persistedResource = PersistedDbResource.Load(db, persistedResourceId.Value);

        if (!persistedResource.IsValid()) return null;
        if (persistedResource.IsExpired) return null;

        var hash = _identifierToHash(innerResourceIdentifier);
        if (!persistedResource.ResourceIdentifierHash.Equals(hash)) return null;

        var bytes = persistedResource.Data;

        return new Resource<byte[]>
        {
            Data = bytes,
            ExpiresAt = persistedResource.ExpiresAt,
        };
    }

    private async ValueTask<Resource<byte[]>> SaveResource(ValueTuple<EntityId, TResourceIdentifier> resourceIdentifier, CancellationToken cancellationToken)
    {
        var resource = await _innerLoader.LoadResourceAsync(resourceIdentifier.Item2, cancellationToken);

        using var tx = _connection.BeginTransaction();
        var tmpId = _partitionId.HasValue ? tx.TempId(_partitionId.Value) : tx.TempId();

        var persisted = new PersistedDbResource.New(tx, tmpId)
        {
            Data = resource.Data,
            ExpiresAt = resource.ExpiresAt,
            ResourceIdentifierHash = _identifierToHash(resourceIdentifier.Item2),
        };

        _referenceAttribute.Add(tx, resourceIdentifier.Item1, persisted);
        await tx.Commit();

        return resource;
    }
}

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static partial class ExtensionsMethods
{
    /// <summary>
    /// Persist the resource in the database.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, byte[]> PersistInDb<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, byte[]> inner,
        IConnection connection,
        ReferenceAttribute<PersistedDbResource> referenceAttribute,
        PersistedDbResourceLoader<TResourceIdentifier>.IdentifierToHash identifierToHash,
        PersistedDbResourceLoader<TResourceIdentifier>.IdentifierToEntityId identifierToEntityId,
        Optional<PartitionId> partitionId)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: (connection, referenceAttribute, identifierToHash, identifierToEntityId, partitionId),
            factory: static (input, inner) => new PersistedDbResourceLoader<TResourceIdentifier>(
                connection: input.connection,
                referenceAttribute: input.referenceAttribute,
                identifierToHash: input.identifierToHash,
                identifierToEntityId: input.identifierToEntityId,
                partitionId: input.partitionId,
                innerLoader: inner
            )
        );
    }

    /// <summary>
    /// Persist the resource in the database.
    /// </summary>
    public static IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, byte[]> PersistInDb<TResourceIdentifier>(
        this IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, byte[]> inner,
        IConnection connection,
        ReferenceAttribute<PersistedDbResource> referenceAttribute,
        Func<TResourceIdentifier, Hash> identifierToHash,
        Optional<PartitionId> partitionId)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: (connection, referenceAttribute, identifierToHash, partitionId),
            factory: static (input, inner) => new PersistedDbResourceLoader<ValueTuple<EntityId, TResourceIdentifier>>(
                connection: input.connection,
                referenceAttribute: input.referenceAttribute,
                identifierToHash: tuple => input.identifierToHash(tuple.Item2),
                identifierToEntityId: static tuple => tuple.Item1,
                partitionId: input.partitionId,
                innerLoader: inner
            )
        );
    }
}
