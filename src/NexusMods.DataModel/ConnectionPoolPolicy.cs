using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.ObjectPool;

namespace NexusMods.DataModel;

/// <summary>
/// Simple connection pool policy that opens a connection when it is created
/// since the pool automatically disposes of the connection when it is no longer
/// needed we don't need to do that here.
/// </summary>
public class ConnectionPoolPolicy : IPooledObjectPolicy<SqliteConnection>, IDisposable
{
    private readonly string _connectionString;

    private bool _isDisposed;
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
        Monitor.Enter(_connections);
        var conn = new SqliteConnection(_connectionString);
        conn.Open();

        _connections.Add(conn);
        return conn;
    }

    /// <inheritdoc />
    public bool Return(SqliteConnection obj)
    {
        Monitor.Exit(_connections);
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            lock (_connections)
            {
                foreach (var connection in _connections)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }

            SqliteConnection.ClearAllPools();
        }

        _isDisposed = true;
    }
}
