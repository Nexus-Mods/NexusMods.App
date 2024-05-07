using NetEscapades.EnumGenerators;

namespace NexusMods.Abstractions.Serialization.DataModel;

/// <summary>
/// Represents a category associated with an entity.
///
/// Category is mostly used to determine which section/table of the datastore
/// (database) our information will be stored inside.
/// </summary>
/// <remarks>
///    Limited to 255 in our current implementation due to how we create IDs.
/// </remarks>
[EnumExtensions]
public enum EntityCategory : byte
{
    /// <summary>
    /// Persisted workspaces.
    /// </summary>
    Workspaces = 19,
}
