using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.Resources.DB;

[PublicAPI]
public class PersistedResourceLoader<TResourceIdentifier, TData> : IResourceLoader<TResourceIdentifier, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
{
    public delegate byte[] DataToBytes(TData data);
    public delegate TData BytesToData(byte[] bytes);
    public delegate Hash IdentifierToHash(TResourceIdentifier resourceIdentifier);

    public delegate EntityId IdentifierToEntityId(TResourceIdentifier resourceIdentifier);

    private readonly IConnection _connection;
    private readonly IResourceLoader<TResourceIdentifier, TData> _innerLoader;
    private readonly ReferenceAttribute<PersistedResource> _referenceAttribute;
    private readonly DataToBytes _dataToBytes;
    private readonly BytesToData _bytesToData;
    private readonly IdentifierToHash _identifierToHash;
    private readonly IdentifierToEntityId _identifierToEntityId;
    private readonly AttributeId _referenceAttributeId;
    private readonly Optional<PartitionId> _partitionId;

    public PersistedResourceLoader(
        IConnection connection,
        ReferenceAttribute<PersistedResource> referenceAttribute,
        IdentifierToHash identifierToHash,
        DataToBytes dataToBytes,
        BytesToData bytesToData,
        IdentifierToEntityId identifierToEntityId,
        Optional<PartitionId> partitionId,
        IResourceLoader<TResourceIdentifier, TData> innerLoader)
    {
        _connection = connection;
        _innerLoader = innerLoader;

        _dataToBytes = dataToBytes;
        _bytesToData = bytesToData;

        _identifierToHash = identifierToHash;
        _identifierToEntityId = identifierToEntityId;

        _referenceAttribute = referenceAttribute;
        _referenceAttributeId = _referenceAttribute.GetDbId(_connection.Registry.Id);
        _partitionId = partitionId;
    }

    /// <inheritdoc/>
    public ValueTask<Resource<TData>> LoadResourceAsync(TResourceIdentifier resourceIdentifier, CancellationToken cancellationToken)
    {
        var entityId = _identifierToEntityId(resourceIdentifier);
        var tuple = (entityId, resourceIdentifier);

        var resource = LoadResource(tuple);
        if (resource is not null) return ValueTask.FromResult(resource);
        return SaveResource(tuple, cancellationToken);
    }

    private Resource<TData>? LoadResource(ValueTuple<EntityId, TResourceIdentifier> resourceIdentifier)
    {
        var db = _connection.Db;
        var (entityId, innerResourceIdentifier) = resourceIdentifier;

        var persistedResourceId = Optional<EntityId>.None;
        var indexSegment = db.Datoms(entityId);
        foreach (var datom in indexSegment)
        {
            if (!datom.A.Equals(_referenceAttributeId)) continue;
            persistedResourceId = _referenceAttribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, indexSegment.RegistryId);
        }

        if (!persistedResourceId.HasValue) return null;
        var persistedResource = PersistedResource.Load(db, persistedResourceId.Value);

        if (!persistedResource.IsValid()) return null;
        if (persistedResource.IsExpired) return null;

        var hash = _identifierToHash(innerResourceIdentifier);
        if (!persistedResource.ResourceIdentifierHash.Equals(hash)) return null;

        var bytes = persistedResource.Data;
        var data = _bytesToData(bytes);

        return new Resource<TData>
        {
            Data = data,
            ExpiresAt = persistedResource.ExpiresAt,
        };
    }

    private async ValueTask<Resource<TData>> SaveResource(ValueTuple<EntityId, TResourceIdentifier> resourceIdentifier, CancellationToken cancellationToken)
    {
        var resource = await _innerLoader.LoadResourceAsync(resourceIdentifier.Item2, cancellationToken);
        var bytes = _dataToBytes(resource.Data);

        using var tx = _connection.BeginTransaction();
        var tmpId = _partitionId.HasValue ? tx.TempId(_partitionId.Value) : tx.TempId();

        var persisted = new PersistedResource.New(tx, tmpId)
        {
            Data = bytes,
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
    public static IResourceLoader<TResourceIdentifier, byte[]> Persist<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, byte[]> inner,
        IConnection connection,
        ReferenceAttribute<PersistedResource> referenceAttribute,
        PersistedResourceLoader<TResourceIdentifier, byte[]>.IdentifierToHash identifierToHash,
        PersistedResourceLoader<TResourceIdentifier, byte[]>.IdentifierToEntityId identifierToEntityId,
        Optional<PartitionId> partitionId)
        where TResourceIdentifier : notnull
    {
        return inner.Persist(
            connection: connection,
            referenceAttribute: referenceAttribute,
            identifierToHash: identifierToHash,
            identifierToEntityId: identifierToEntityId,
            dataToBytes: static bytes => bytes,
            bytesToData: static bytes => bytes,
            partitionId: partitionId
        );
    }

    public static IResourceLoader<TResourceIdentifier, TData> Persist<TResourceIdentifier, TData>(
        this IResourceLoader<TResourceIdentifier, TData> inner,
        IConnection connection,
        ReferenceAttribute<PersistedResource> referenceAttribute,
        PersistedResourceLoader<TResourceIdentifier, TData>.IdentifierToHash identifierToHash,
        PersistedResourceLoader<TResourceIdentifier, TData>.IdentifierToEntityId identifierToEntityId,
        PersistedResourceLoader<TResourceIdentifier, TData>.DataToBytes dataToBytes,
        PersistedResourceLoader<TResourceIdentifier, TData>.BytesToData bytesToData,
        Optional<PartitionId> partitionId)
        where TResourceIdentifier : notnull
        where TData : notnull
    {
        return inner.Then(
            state: (connection, referenceAttribute, identifierToHash, identifierToEntityId, dataToBytes, bytesToData, partitionId),
            factory: static (input, inner) => new PersistedResourceLoader<TResourceIdentifier, TData>(
                connection: input.connection,
                referenceAttribute: input.referenceAttribute,
                identifierToHash: input.identifierToHash,
                identifierToEntityId: input.identifierToEntityId,
                dataToBytes: input.dataToBytes,
                bytesToData: input.bytesToData,
                partitionId: input.partitionId,
                innerLoader: inner
            )
        );
    }
}
