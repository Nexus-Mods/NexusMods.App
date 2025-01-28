using TransparentValueObjects;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// A sortable migration id
/// </summary>
[ValueObject<ushort>]
public readonly partial struct MigrationId : IAugmentWith<JsonAugment>
{
    /// <summary>
    /// Parse the MigrationId and Migration name from the given class name, should be in the format of _XXXX_Name
    /// </summary>
    public static (MigrationId Id, string Name) ParseNameAndId(string name)
    {
        var split = name.Split("_", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (From(ushort.Parse(split[0])), split[1]);
    }
}
