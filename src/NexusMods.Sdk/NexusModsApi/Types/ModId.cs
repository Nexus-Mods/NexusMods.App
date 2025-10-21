using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types.V2;

/// <summary>
/// Identifier for a mod on Nexus Mods.
/// </summary>
[ValueObject<uint>]
public readonly partial struct ModId : IAugmentWith<DefaultValueAugment>, IAugmentWith<JsonAugment>
{
    /// <inheritdoc/>
    public static ModId DefaultValue => From(0);
}

/// <summary>
/// Attribute for <see cref="ModId"/>.
/// </summary>
public class ModIdAttribute(string ns, string name) : ScalarAttribute<ModId, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(ModId value) => value.Value;

    /// <inheritdoc />
    protected override ModId FromLowLevel(uint value, AttributeResolver resolver) => ModId.From(value);
} 
