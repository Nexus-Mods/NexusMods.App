using Microsoft.Data.Sqlite;
using Microsoft.Extensions.ObjectPool;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class ConnectionPoolPolicy : IPooledObjectPolicy<SqliteConnection>
{
    private readonly string _connectionString;

    public ConnectionPoolPolicy(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqliteConnection Create()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public bool Return(SqliteConnection obj)
    {
        return true;
    }
}
