using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Resources.DB;

[PublicAPI]
public sealed class IdentifierLoader<TResourceIdentifier, TData> : IResourceLoader<EntityId, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
{
    private readonly IConnection _connection;
    private readonly IReadableAttribute<TResourceIdentifier> _attribute;
    private readonly AttributeId _attributeId;

    private readonly IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> _innerLoader;

    /// <summary>
    /// Constructor.
    /// </summary>
    public IdentifierLoader(
        IConnection connection,
        IReadableAttribute<TResourceIdentifier> attribute,
        IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> innerLoader)
    {
        _connection = connection;
        _innerLoader = innerLoader;

        _attribute = attribute;
        _attributeId = _connection.AttributeCache.GetAttributeId(attribute.Id);
    }

    /// <inheritdoc/>
    public ValueTask<Resource<TData>> LoadResourceAsync(EntityId entityId, CancellationToken cancellationToken)
    {
        var resourceIdentifier = GetIdentifier(entityId);
        return _innerLoader.LoadResourceAsync((entityId, resourceIdentifier), cancellationToken);
    }

    private TResourceIdentifier GetIdentifier(EntityId entityId)
    {
        var indexSegment = _connection.Db.Get(entityId);
        foreach (var datom in indexSegment)
        {
            if (!datom.A.Equals(_attributeId)) continue;
            var value = _attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, _connection.AttributeResolver);
            return value;
        }

        throw new KeyNotFoundException($"Unable to find a value in Entity `{entityId}` with attribute `{_attribute}`");
    }
}

public static partial class ExtensionsMethods
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
}
