using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// Updates the state of a database and provides hooks for migrating schemas
/// and transforming data between versions.
/// </summary>
public class MigrationService
{
    private readonly ILogger<MigrationService> _logger;
    private readonly IConnection _connection;
    private readonly List<IMigration> _migrations;

    public MigrationService(ILogger<MigrationService> logger, IConnection connection, IEnumerable<IMigration> migrations)
    {
        _logger = logger;
        _connection = connection;
        _migrations = migrations.OrderBy(m => m.CreatedAt).ToList();
    }

    public async Task Run()
    {
        // Run all migrations, for now this interface works by handing a transaction to each migration, in the future we'll need
        // to add support for changing history of the datoms and not just the most recent state. But until we need such a migration
        // we'll go with this approach as it's simpler.
        foreach (var migration in _migrations)
        {
            var db = _connection.Db;
            if (!migration.ShouldRun(db))
            {
                _logger.LogInformation("Migration {Name} skipped", migration.Name);
                continue;
            }
            
            _logger.LogInformation("Running migration {Name}", migration.Name);
            using var tx = _connection.BeginTransaction();
            migration.Migrate(db, tx);
            await tx.Commit();
            _logger.LogInformation("Migration {Name} completed", migration.Name);
        }
    }
}
