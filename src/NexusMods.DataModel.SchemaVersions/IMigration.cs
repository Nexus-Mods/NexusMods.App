using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// A definition of a single data migration
/// </summary>
public interface IMigration
{
    /// <summary>
    /// The unique sequential id of this migration and the human friendly name. These values are stored in this way
    /// so that they can be easily extracted from the class name.
    /// </summary>
    public static abstract (MigrationId Id, string Name) IdAndName { get; }
    
    /// <summary>
    /// Perform initial lookups and prepare for the migration
    /// </summary>
    Task Prepare(IDb db);
}
