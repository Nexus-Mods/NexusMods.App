using Microsoft.Data.Sqlite;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Extensions;

public static class SqliteParameterCollectionExtensions
{
    public static void AddWithValueUntagged(this SqliteParameterCollection collection, string name, IId id)
    {
        var idBytes = new byte[id.SpanSize];
        id.ToSpan(idBytes.AsSpan());
        collection.AddWithValue(name, idBytes);
    }

    public static void AddWithValueTagged(this SqliteParameterCollection collection, string name, IId id)
    {
        var idBytes = new byte[id.SpanSize + 1];
        id.ToTaggedSpan(idBytes.AsSpan());
        collection.AddWithValue(name, idBytes);
    }

}
