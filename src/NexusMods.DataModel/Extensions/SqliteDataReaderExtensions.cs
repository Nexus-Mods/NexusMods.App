using Microsoft.Data.Sqlite;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using SQLitePCL;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extensions for SqliteDataReader
/// </summary>
public static class SqliteDataReaderExtensions
{
    /// <summary>
    /// Gets an IId from a column in a SqliteDataReader, using the EntityCategory to determine
    /// the entity type
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="ent"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public static IId GetId(this SqliteDataReader reader, EntityCategory ent, int column)
    {
        return IId.FromSpan(ent, reader.GetBlob(column));
    }

    /// <summary>
    /// Gets an IId from a column in a SqliteDataReader, using the tag in the
    /// data to determine the entity type
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="ent"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public static IId GetId(this SqliteDataReader reader, int column)
    {
        return IId.FromTaggedSpan(reader.GetBlob(column));
    }

    /// <summary>
    /// Gets a blob from a column in a SqliteDataReader as a ReadOnlySpan
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public static ReadOnlySpan<byte> GetBlob(this SqliteDataReader reader, int column)
    {
        return raw.sqlite3_column_blob(reader.Handle, column);
    }
}
