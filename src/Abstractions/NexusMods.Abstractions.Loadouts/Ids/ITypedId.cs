using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts.Ids;

public interface ITypedId<TModel> : ITypedEntityId where TModel : IEntity
{
    /// <summary>
    /// Resolves the entity from the database.
    /// </summary>
    public TModel Resolve(IDb db) => db.Get<TModel>(Value);
}
