using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Migrations;

/// <summary>
/// A definition of a single data migration
/// </summary>
public interface IMigration
{
    /// <summary>
    /// The name of the migration
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// A long description of the migration
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// A date for the migration's creation. Not used for anything other than sorting. Migrations
    /// will be run in order of this date.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }
    
    /// <summary>
    /// Returns true if the migration should run. This function should do any sort of querying and processing to make sure
    /// data is in the format expected by the migration.
    /// </summary>
    public bool ShouldRun(IDb db);

    /// <summary>
    /// Runs the migration
    /// </summary>
    public void Migrate(IDb basis, ITransaction tx);
}
