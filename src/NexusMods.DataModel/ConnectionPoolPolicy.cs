using Microsoft.Data.Sqlite;
using Microsoft.Extensions.ObjectPool;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Simple connection pool policy that opens a connection when it is created
/// since the pool automatically disposes of the connection when it is no longer
/// needed we don't need to do that here.
/// </summary>
public class ConnectionPoolPolicy : IPooledObjectPolicy<SqliteConnection>, IDisposable
{
    private readonly string _connectionString;

    private List<SqliteConnection> _connections = new();

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
        _connections.Add(conn);
        return conn;
    }

    /// <inheritdoc />
    public bool Return(SqliteConnection obj)
    {
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var conn in _connections)
        {
            conn.Close();
            conn.Dispose();
        }

        SqliteConnection.ClearAllPools();
    }
}
