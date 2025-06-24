using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Attribute for <see cref="Sha1Value"/>.
/// </summary>
public class Sha1Attribute(string ns, string name) : ScalarAttribute<Sha1Value, Memory<byte>, BlobSerializer>(ns, name)
{
    /// <inheritdoc/>
    protected override Memory<byte> ToLowLevel(Sha1Value value) => value.AsSpan().ToArray();

    /// <inheritdoc/>
    protected override Sha1Value FromLowLevel(Memory<byte> value, AttributeResolver resolver) => Sha1Value.From(value.Span);
}
