using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// Identifier for a file on Nexus Mods.
/// </summary>
[ValueObject<uint>]
public readonly partial struct FileId : IAugmentWith<DefaultValueAugment>, IAugmentWith<JsonAugment>
{
    /// <inheritdoc/>
    public static FileId DefaultValue => From(0);
}

/// <summary>
/// Attribute for <see cref="FileId"/>.
/// </summary>
public class FileIdAttribute(string ns, string name) : ScalarAttribute<FileId, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(FileId value) => value.Value;

    /// <inheritdoc />
    protected override FileId FromLowLevel(uint value, AttributeResolver resolver) => FileId.From(value);
}
