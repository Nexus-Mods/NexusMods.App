using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Sdk.NexusModsApi;

/// <summary>
/// Identifier for a game on Nexus Mods.
/// </summary>
[ValueObject<uint>]
public readonly partial struct NexusModsGameId : IAugmentWith<DefaultValueAugment>, IAugmentWith<JsonAugment>
{
    /// <inheritdoc/>
    public static NexusModsGameId DefaultValue => From(0);
}

/// <summary>
/// Attribute for <see cref="NexusModsGameId"/>.
/// </summary>
public class NexusModsGameIdAttribute(string ns, string name) : ScalarAttribute<NexusModsGameId, uint, UInt32Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override uint ToLowLevel(NexusModsGameId value) => value.Value;

    /// <inheritdoc />
    protected override NexusModsGameId FromLowLevel(uint value, AttributeResolver resolver) => NexusModsGameId.From(value);
}
