using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// An attribute that holds a <see cref="RevisionId"/> value.
/// </summary>
public class RevisionIdAttribute(string ns, string name) : ScalarAttribute<RevisionId, ulong, UInt64Serializer>(ns, name)
{
    /// <inheritdoc />
    public override ulong ToLowLevel(RevisionId value) => value.Value;

    /// <inheritdoc />
    public override RevisionId FromLowLevel(ulong value, AttributeResolver resolver) => RevisionId.From(value);
}
