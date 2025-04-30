using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;
namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// An individual mod ID. Unique per <see cref="GameId"/>.
/// i.e. Each game has its own set of IDs and starts with 0.
/// </summary>
[ValueObject<uint>] // Matches backend. Do not change.
public readonly partial struct ModId : IAugmentWith<DefaultValueAugment>, IAugmentWith<JsonAugment>
{
    /// <inheritdoc/>
    public static ModId DefaultValue => From(default(uint));
}


/// <summary>
/// Mod ID attribute, for NexusMods API mod IDs.
/// </summary>
public class ModIdAttribute(string ns, string name) 
    : ScalarAttribute<ModId, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(ModId value) => value.Value;

    /// <inheritdoc />
    protected override ModId FromLowLevel(uint value, AttributeResolver resolver) => ModId.From(value);
} 
