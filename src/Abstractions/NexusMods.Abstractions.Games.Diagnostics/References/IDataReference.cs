using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
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
    /// Gets the TxId of the data store at the time of the reference.
    /// </summary>
    /// <remarks>
    /// This is used for change tracking.
    /// </remarks>
    TxId TxId { get; }

    /// <summary>
    /// Resolves the data at <see cref="TxId"/> to an <see cref="Entity"/>.
    /// </summary>
    /// <returns><c>null</c> if the value doesn't exist in the data store.</returns>
    IReadOnlyModel? ResolveData(IServiceProvider serviceProvider, TxId dataStore);

    /// <summary>
    /// Converts the data from <see cref="ResolveData"/> to a string representation.
    /// </summary>
    string ToStringRepresentation(IReadOnlyModel data);
}

/// <summary>
/// Represents a reference to some data in the data store.
/// </summary>
/// <seealso cref="IDataReference"/>
[PublicAPI]
public interface IDataReference<out TDataId, TData> : IDataReference
    where TData : IReadOnlyModel
{
    /// <summary>
    /// Gets the ID of the referenced data.
    /// </summary>
    TDataId DataId { get; }

    /// <inheritdoc/>
    IReadOnlyModel? IDataReference.ResolveData(IServiceProvider serviceProvider, TxId dataStore) 
        => ResolveData(serviceProvider, dataStore);

    /// <inheritdoc/>
    string IDataReference.ToStringRepresentation(IReadOnlyModel data)
    {
        if (data is not TData actualData)
            throw new ArgumentException($"Argument is not of type '{typeof(TData)}' but '{data.GetType()}'", nameof(data));
        return ToStringRepresentation(actualData);
    }

    /// <summary>
    /// Resolves the data at <see cref="IDataReference.DataStoreId"/>.
    /// </summary>
    /// <returns><c>null</c> if the value doesn't exist in the data store.</returns>
    new TData? ResolveData(IServiceProvider serviceProvider, IConnection dataStore);

    /// <summary>
    /// Converts the data from <see cref="ResolveData"/> to a string representation.
    /// </summary>
    string ToStringRepresentation(TData data);
}
