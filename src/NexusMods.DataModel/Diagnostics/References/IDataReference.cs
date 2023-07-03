using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Diagnostics.References;

/// <summary>
/// A reference to some data by ID.
/// </summary>
[PublicAPI]
public interface IDataReference
{
    /// <summary>
    /// Gets the type of the ID used to reference the data.
    /// </summary>
    Type IdType { get; }

    /// <summary>
    /// Gets the type of the data being referenced.
    /// </summary>
    Type DataType { get; }
}

/// <summary>
/// A reference to some data by ID.
/// </summary>
[PublicAPI]
public interface IDataReference<out TDataId, TData> : IDataReference
    where TData : Entity
{
    Type IDataReference.IdType => typeof(TDataId);

    Type IDataReference.DataType => typeof(TData);

    /// <summary>
    /// Gets the ID of the referenced data.
    /// </summary>
    TDataId DataId { get; }
}
