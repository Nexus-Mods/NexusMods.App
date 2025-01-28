using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.DataModel.SchemaVersions;

/// <summary>
/// An attribute for storing the migration id of a schema version.
/// </summary>
public class MigrationIdAttribute(string ns, string name) : ScalarAttribute<MigrationId, ushort, UInt16Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override ushort ToLowLevel(MigrationId value)
    {
        return value.Value;
    }

    /// <inheritdoc />
    protected override MigrationId FromLowLevel(ushort value, AttributeResolver resolver)
    {
        return MigrationId.From(value);
    }
}
