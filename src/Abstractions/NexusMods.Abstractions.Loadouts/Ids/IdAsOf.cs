using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts.Ids;

public struct IdAsOf<TId, TModel> 
    where TId : ITypedId<TModel> 
    where TModel : IEntity
{
    /// <summary>
    /// Create a new <see cref="IdAsOf{TId,TModel}"/>.
    /// </summary>
    public static IdAsOf<TId, TModel> Create(TxId asOf, TId id)
        => new()
        {
            AsOf = asOf,
            Id = id,
        };
    
    /// <summary>
    /// The asof transaction id.
    /// </summary>
    public TxId AsOf { get; init; }

    /// <summary>
    /// The id of the entity.
    /// </summary>
    public TId Id { get; init; }

    /// <summary>
    /// Resolve the entity from the database asof the given transaction id.
    /// </summary>
    public TModel Resolve(IConnection connection) 
        => connection.AsOf(AsOf).Get<TModel>(Id.Value);
}
