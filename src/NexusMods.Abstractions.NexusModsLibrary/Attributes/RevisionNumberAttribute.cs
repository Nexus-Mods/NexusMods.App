using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// An attribute that holds a <see cref="RevisionNumber"/> value.
/// </summary>
public class RevisionNumberAttribute(string ns, string name) : ScalarAttribute<RevisionNumber, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    public override ulong ToLowLevel(RevisionNumber value) => value.Value;

    /// <inheritdoc />
    public override RevisionNumber FromLowLevel(ulong value, AttributeResolver resolver) => RevisionNumber.From(value);
}
