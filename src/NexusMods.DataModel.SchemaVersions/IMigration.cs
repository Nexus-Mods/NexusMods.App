using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// A definition of a single data migration
/// </summary>
public interface IMigration
{
    /// <summary>
    /// The unique sequential id of this migration and the human friendly name
    /// </summary>
    public static abstract (MigrationId Id, string Name) IdAndName { get; }
    
    /// <summary>
    /// Do any initial processing required to start the migration. Data can be stored 
    /// </summary>
    public void Prepare(IDb db);
}
