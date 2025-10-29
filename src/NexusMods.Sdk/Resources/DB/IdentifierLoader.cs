using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Sdk.Resources;

/// <summary>
/// Represents a loader that transforms an <see cref="EntityId"/> into a resource identifier
/// and subsequently retrieves the corresponding resource data.
/// </summary>
[PublicAPI]
public sealed class IdentifierLoader<TResourceIdentifier, TData> : IResourceLoader<EntityId, TData>
    where TResourceIdentifier : notnull
    where TData : notnull
{
    private readonly IConnection _connection;
    private readonly IReadableAttribute<TResourceIdentifier> _attribute;
    private readonly AttributeId _attributeId;

    private readonly Optional<TData> _fallbackValue;
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

    /// <summary>
    /// Constructor.
    /// </summary>
    public IdentifierLoader(
        IConnection connection,
        TData fallbackValue,
        IReadableAttribute<TResourceIdentifier> attribute,
        IResourceLoader<ValueTuple<EntityId, TResourceIdentifier>, TData> innerLoader) : this(connection, attribute, innerLoader)
    {
        _fallbackValue = fallbackValue;
    }

    /// <inheritdoc/>
    public ValueTask<Resource<TData>> LoadResourceAsync(EntityId entityId, CancellationToken cancellationToken)
    {
        var optional = GetIdentifier(entityId);
        if (!optional.HasValue && !_fallbackValue.HasValue)
            throw new KeyNotFoundException($"Unable to find a value in Entity `{entityId}` with attribute `{_attribute}`");

        if (optional.HasValue) return _innerLoader.LoadResourceAsync((entityId, optional.Value), cancellationToken);
        return ValueTask.FromResult(new Resource<TData>
        {
            Data = _fallbackValue.Value,
        });
    }

    private Optional<TResourceIdentifier> GetIdentifier(EntityId entityId)
    {
        var indexSegment = _connection.Db[entityId];
        if (indexSegment.TryGetOne(_attribute, out var resolved))
            return (TResourceIdentifier)_attribute.FromLowLevelObject(resolved, _connection.AttributeResolver);
        
        return Optional<TResourceIdentifier>.None;
    }
}
