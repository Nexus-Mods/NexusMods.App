using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;
namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// Unique ID for a mod file associated with a game (<see cref="GameId"/>).
/// Querying mod pages returns items of this type.
/// </summary>
[ValueObject<uint>] // Matches backend. Do not change.
public readonly partial struct FileId : IAugmentWith<DefaultValueAugment>, IAugmentWith<JsonAugment>
{
    /// <inheritdoc/>
    public static FileId DefaultValue => From(default(uint));
}

/// <summary>
/// File ID attribute, for NexusMods API file IDs.
/// </summary>
public class FileIdAttribute(string ns, string name) : 
    ScalarAttribute<FileId, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(FileId value) => value.Value;

    /// <inheritdoc />
    protected override FileId FromLowLevel(uint value, AttributeResolver resolver) => FileId.From(value);
}
