using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Attribute for <see cref="Md5Value"/>.
/// </summary>
public class Md5Attribute(string ns, string name) : ScalarAttribute<Md5Value, Memory<byte>, BlobSerializer>(ns, name)
{
    /// <inheritdoc/>
    protected override Memory<byte> ToLowLevel(Md5Value value) => value.AsSpan().ToArray();

    /// <inheritdoc/>
    protected override Md5Value FromLowLevel(Memory<byte> value, AttributeResolver resolver) => Md5Value.From(value.Span);
}
