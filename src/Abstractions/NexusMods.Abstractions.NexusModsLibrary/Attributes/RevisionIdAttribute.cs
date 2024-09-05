using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// An attribute that holds a <see cref="RevisionId"/> value.
/// </summary>
public class RevisionIdAttribute(string ns, string name) : ScalarAttribute<RevisionId, ulong>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(RevisionId value)
    {
        return value.Value;
    }

    /// <inheritdoc />
    protected override RevisionId FromLowLevel(ulong value, ValueTags tags, RegistryId registryId)
    {
        return RevisionId.From(value);
    }
}
