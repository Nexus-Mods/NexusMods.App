using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly List<MigrationDefinition> _migrations;
    private readonly IServiceProvider _provider;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public MigrationService(ILogger<MigrationService> logger, IConnection connection, IServiceProvider provider, IEnumerable<MigrationDefinition> migrations)
    {
        _logger = logger;
        _connection = connection;
        _provider = provider;
        _migrations = migrations.OrderBy(m => m.Id).ToList();
    }

    /// <summary>
    /// Return the current schema version (0 if none exists)
    /// </summary>
    private ushort GetCurrentMigrationId(IDb db)
    {
        var version = SchemaVersion.All(db).SingleOrDefault();
        return !version.IsValid() ? (ushort)0 : version.CurrentVersion.Value;
    }

    public async Task<IDb> InitialSetup()
    {
        var currentVersion = GetCurrentMigrationId(_connection.Db);
        if (currentVersion != 0)
            throw new InvalidOperationException("Cannot perform a schema init on a database that already has a schema version");

        using var tx = _connection.BeginTransaction();
        _ = new SchemaVersion.New(tx)
        {
            CurrentVersion = _migrations.Last().Id,
        };

        // Populate the log, this isn't strictly needed but is nice for testing, to know what migrations we saw when the DB was created
        foreach (var migration in _migrations)
        {
            _ = new MigrationLogItem.New(tx)
            {
                MigrationId = migration.Id,
                RunAt = DateTimeOffset.Now,
                WasRun = false,
            };
        }
        
        var result = await tx.Commit();
        return result.Db;
    }

    /// <summary>
    /// Perform all  the DB migrations
    /// </summary>
    public async Task MigrateAll()
    {
        var totalStopWatch = Stopwatch.StartNew();
        var numMigrations = 0;

        var currentVersion = GetCurrentMigrationId(_connection.Db);
        foreach (var definition in _migrations.Where(m => m.Id > currentVersion))
        {
            numMigrations++;

            var instance = (IMigration)_provider.GetRequiredService(definition.Type);

            _logger.LogInformation("Running Migration `{Id}: {Name}`", definition.Id, definition.Name);

            var prepareStopWatch = Stopwatch.StartNew();
            await instance.Prepare(_connection.Db);
            var prepareDuration = prepareStopWatch.Elapsed;

            var runStopWatch = Stopwatch.StartNew();

            switch (instance)
            {
                case IScanningMigration scanningMigration:
                {
                    await _connection.ScanUpdate(scanningMigration.Update);
                    using var tx = _connection.BeginTransaction();
                    _ = new MigrationLogItem.New(tx)
                    {
                        RunAt = DateTimeOffset.UtcNow,
                        MigrationId = definition.Id,
                        WasRun = true,
                    };
                    var id = SchemaVersionEntityId(_connection.Db, tx, definition.Id);
                    tx.Add(id, SchemaVersion.CurrentVersion, definition.Id);
                    await tx.Commit();
                    break;
                }
                case ITransactionalMigration transactionalMigration:
                {
                    using var tx = _connection.BeginTransaction();
                    transactionalMigration.Migrate(tx, _connection.Db);
                    _ = new MigrationLogItem.New(tx)
                    {
                        RunAt = DateTimeOffset.UtcNow,
                        MigrationId = definition.Id,
                        WasRun = true,
                    };
                    
                    var id = SchemaVersionEntityId(_connection.Db, tx, definition.Id);
                    tx.Add(id, SchemaVersion.CurrentVersion, definition.Id);
                    await tx.Commit();
                    break;
                }
                default:
                    throw new NotImplementedException("No other migration types supported (yet)");
            }

            var runDuration = runStopWatch.Elapsed;
            var migrationDuration = prepareDuration + runDuration;

            _logger.LogDebug("Migration `{Id}: {Name}` took `{PrepareDuration}` ms to prepare, `{RunDuration}` ms to run for a total of `{MigrationDuration}` ms", definition.Id, definition.Name, prepareDuration.TotalMilliseconds, runDuration.TotalMilliseconds, migrationDuration.TotalMilliseconds);
        }

        var totalDuration = totalStopWatch.Elapsed;
        _logger.LogInformation("Ran `{Count}` migration(s) in `{Duration}` seconds", numMigrations, totalDuration.TotalSeconds);
    }

    private EntityId SchemaVersionEntityId(IDb db, ITransaction tx, MigrationId definitionId)
    {
        var existing = SchemaVersion.All(db).SingleOrDefault();
        if (existing.IsValid())
            return existing.Id;
        return tx.TempId();
    }
}
