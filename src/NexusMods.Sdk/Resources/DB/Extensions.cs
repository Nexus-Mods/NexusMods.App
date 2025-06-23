using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Sdk.Resources;

public static partial class Extensions
{
    public static IResourceLoader<EntityId, TData> EntityIdToIdentifier<TResourceIdentifier, TData>(
        this IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> inner,
        IConnection connection,
        IReadableAttribute<TResourceIdentifier> attribute)
        where TResourceIdentifier : notnull
        where TData : notnull
    {
        return inner.Then(
            state: (connection, attribute),
            factory: static (input, inner) => new IdentifierLoader<TResourceIdentifier,TData>(
                connection: input.connection,
                attribute: input.attribute,
                innerLoader: inner
            )
        );
    }

    public static IResourceLoader<EntityId, TData> EntityIdToOptionalIdentifier<TResourceIdentifier, TData>(
        this IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> inner,
        IConnection connection,
        TData fallbackValue,
        IReadableAttribute<TResourceIdentifier> attribute)
        where TResourceIdentifier : notnull
        where TData : notnull
    {
        return inner.Then(
            state: (connection, fallbackValue, attribute),
            factory: static (input, inner) => new IdentifierLoader<TResourceIdentifier,TData>(
                connection: input.connection,
                fallbackValue: input.fallbackValue,
                attribute: input.attribute,
                innerLoader: inner
            )
        );
    }

        /// <summary>
    /// Persist the resource in the database.
    /// </summary>
    public static IResourceLoader<TResourceIdentifier, byte[]> PersistInDb<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, byte[]> inner,
        IConnection connection,
        ReferenceAttribute<PersistedDbResource> referenceAttribute,
        PersistedDbResourceLoader<TResourceIdentifier>.IdentifierToHash identifierToHash,
        PersistedDbResourceLoader<TResourceIdentifier>.IdentifierToEntityId identifierToEntityId,
        Optional<PartitionId> partitionId,
        Optional<TimeSpan> expiresAfter = default)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: (connection, referenceAttribute, identifierToHash, identifierToEntityId, partitionId, expiresAfter),
            factory: static (input, inner) => new PersistedDbResourceLoader<TResourceIdentifier>(
                connection: input.connection,
                referenceAttribute: input.referenceAttribute,
                identifierToHash: input.identifierToHash,
                identifierToEntityId: input.identifierToEntityId,
                partitionId: input.partitionId,
                expiresAfter: input.expiresAfter,
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
        Optional<PartitionId> partitionId,
        Optional<TimeSpan> expiresAfter = default)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: (connection, referenceAttribute, identifierToHash, partitionId, expiresAfter),
            factory: static (input, inner) => new PersistedDbResourceLoader<ValueTuple<EntityId, TResourceIdentifier>>(
                connection: input.connection,
                referenceAttribute: input.referenceAttribute,
                identifierToHash: tuple => input.identifierToHash(tuple.Item2),
                identifierToEntityId: static tuple => tuple.Item1,
                partitionId: input.partitionId,
                expiresAfter: input.expiresAfter,
                innerLoader: inner
            )
        );
    }
}
