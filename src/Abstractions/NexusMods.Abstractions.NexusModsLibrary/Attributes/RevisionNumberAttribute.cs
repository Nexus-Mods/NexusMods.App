using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.NexusModsLibrary.Attributes;

/// <summary>
/// An attribute that holds a <see cref="RevisionNumber"/> value.
/// </summary>
public class RevisionNumberAttribute(string ns, string name) : ScalarAttribute<RevisionNumber, ulong>(ValueTag.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(RevisionNumber value) => value.Value;

    /// <inheritdoc />
    protected override RevisionNumber FromLowLevel(ulong value, AttributeResolver resolver) => RevisionNumber.From(value);
}
