using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Resources.DB;

[PublicAPI]
public class IdentifierLoader<TResourceIdentifier, TData, TLowerLevel> : IResourceLoader<EntityId, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
    where TLowerLevel : notnull
{
    private readonly IConnection _connection;
    private readonly Attribute<TResourceIdentifier, TLowerLevel> _attribute;
    private readonly AttributeId _attributeId;

    private readonly IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> _innerLoader;

    /// <summary>
    /// Constructor.
    /// </summary>
    public IdentifierLoader(
        IConnection connection,
        Attribute<TResourceIdentifier, TLowerLevel> attribute,
        IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> innerLoader)
    {
        _connection = connection;
        _innerLoader = innerLoader;

        _attribute = attribute;
        _attributeId = attribute.GetDbId(_connection.Registry.Id);
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
            var value = _attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, indexSegment.RegistryId);
            return value;
        }

        throw new KeyNotFoundException($"Unable to find a value in Entity `{entityId}` with attribute `{_attribute}`");
    }
}

public static partial class ExtensionsMethods
{
    public static IResourceLoader<EntityId, TData> EntityIdToIdentifier<TResourceIdentifier, TData, TLowerLevel>(
        this IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> inner,
        IConnection connection,
        Attribute<TResourceIdentifier, TLowerLevel> attribute)
        where TResourceIdentifier : notnull
        where TData : notnull
        where TLowerLevel : notnull
    {
        return inner.Then(
            state: (connection, attribute),
            factory: static (input, inner) => new IdentifierLoader<TResourceIdentifier,TData,TLowerLevel>(
                connection: input.connection,
                attribute: input.attribute,
                innerLoader: inner
            )
        );
    }
}
