
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// Information about a migration that has been applied to the database
/// </summary>
public partial class MigrationLogItem : IModelDefinition
{
    private static string Namespace => "NexusMods.DataModel.SchemaVersions.MigrationLogItem";
    
    /// <summary>
    /// The time when the migration was run
    /// </summary>
    public static readonly TimestampAttribute RunAt = new(Namespace, nameof(RunAt));
    
    /// <summary>
    /// The migration that was run
    /// </summary>
    public static readonly MigrationIdAttribute MigrationId = new(Namespace, nameof(MigrationId));
    
    /// <summary>
    /// True if this migration was run, false if it was just inserted as part of the creation of new database
    /// </summary>
    public static readonly BooleanAttribute WasRun = new(Namespace, nameof(WasRun));
}
