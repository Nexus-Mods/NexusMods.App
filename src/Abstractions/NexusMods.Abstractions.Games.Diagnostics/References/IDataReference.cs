using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Diagnostics.References;

/// <summary>
/// Represents a reference to some data in the data store.
/// </summary>
/// <seealso cref="IDataReference{TDataId,TData}"/>
[PublicAPI]
public interface IDataReference
{
    /// <summary>
    /// Gets the ID of the <see cref="Entity"/> in the data store.
    /// </summary>
    /// <remarks>
    /// This is used for change tracking.
    /// </remarks>
    IId DataStoreId { get; }

    /// <summary>
    /// Resolves the data at <see cref="DataStoreId"/>.
    /// </summary>
    /// <returns><c>null</c> if the value doesn't exist in the data store.</returns>
    Entity? ResolveData(IServiceProvider serviceProvider, IDataStore dataStore);

    /// <summary>
    /// Converts the data from <see cref="ResolveData"/> to a string representation.
    /// </summary>
    string ToStringRepresentation(Entity data);
}

/// <summary>
/// Represents a reference to some data in the data store.
/// </summary>
/// <seealso cref="IDataReference"/>
[PublicAPI]
public interface IDataReference<out TDataId, TData> : IDataReference
    where TData : Entity
{
    /// <summary>
    /// Gets the ID of the referenced data.
    /// </summary>
    TDataId DataId { get; }

    /// <inheritdoc/>
    Entity? IDataReference.ResolveData(IServiceProvider serviceProvider, IDataStore dataStore) => ResolveData(serviceProvider, dataStore);

    /// <inheritdoc/>
    string IDataReference.ToStringRepresentation(Entity data)
    {
        if (data is not TData actualData)
            throw new ArgumentException($"Argument is not of type '{typeof(TData)}' but '{data.GetType()}'", nameof(data));
        return ToStringRepresentation(actualData);
    }

    /// <summary>
    /// Resolves the data at <see cref="IDataReference.DataStoreId"/>.
    /// </summary>
    /// <returns><c>null</c> if the value doesn't exist in the data store.</returns>
    new TData? ResolveData(IServiceProvider serviceProvider, IDataStore dataStore);

    /// <summary>
    /// Converts the data from <see cref="ResolveData"/> to a string representation.
    /// </summary>
    string ToStringRepresentation(TData data);
}
