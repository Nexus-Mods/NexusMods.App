using Microsoft.Data.Sqlite;
using Microsoft.Extensions.ObjectPool;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Simple connection pool policy that opens a connection when it is created
/// since the pool automatically disposes of the connection when it is no longer
/// needed we don't need to do that here.
/// </summary>
public class ConnectionPoolPolicy : IPooledObjectPolicy<SqliteConnection>
{
    private readonly string _connectionString;

    /// <summary>
    /// Constructor that takes the connection string used when creating new connections
    /// </summary>
    /// <param name="connectionString"></param>
    public ConnectionPoolPolicy(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public SqliteConnection Create()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <inheritdoc />
    public bool Return(SqliteConnection obj)
    {
        return true;
    }
}
