namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// A definition for a schema migration
/// </summary>
public record MigrationDefinition(MigrationId Id, string Name, Type Type);
