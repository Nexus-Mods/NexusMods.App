using Microsoft.Data.Sqlite;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extension methods for SqliteParameterCollection
/// </summary>
public static class SqliteParameterCollectionExtensions
{
    /// <summary>
    /// Adds a parameter to the collection with the given name and an id value
    /// stores the id as a blob without a tag
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="name"></param>
    /// <param name="id"></param>
    public static void AddWithValueUntagged(this SqliteParameterCollection collection, string name, IId id)
    {
        var idBytes = new byte[id.SpanSize];
        id.ToSpan(idBytes.AsSpan());
        collection.AddWithValue(name, idBytes);
    }

    /// <summary>
    /// Adds a parameter to the collection with the given name and an id value
    /// stores the id as a blob with a tag
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="name"></param>
    /// <param name="id"></param>
    public static void AddWithValueTagged(this SqliteParameterCollection collection, string name, IId id)
    {
        var idBytes = new byte[id.SpanSize + 1];
        id.ToTaggedSpan(idBytes.AsSpan());
        collection.AddWithValue(name, idBytes);
    }

}
