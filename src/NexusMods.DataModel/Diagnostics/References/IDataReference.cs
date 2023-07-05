using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Diagnostics.References;

/// <summary>
/// A reference to some data by ID.
/// </summary>
[PublicAPI]
public interface IDataReference { }

/// <summary>
/// A reference to some data by ID.
/// </summary>
[PublicAPI]
public interface IDataReference<out TDataId, TData> : IDataReference
    where TData : Entity
{
    /// <summary>
    /// Gets the ID of the referenced data.
    /// </summary>
    TDataId DataId { get; }

    /// <summary>
    /// Gets the ID of the <see cref="Entity"/> in the data store.
    /// </summary>
    /// <remarks>
    /// This is used for change tracking.
    /// </remarks>
    IId DataStoreId { get; }
}
