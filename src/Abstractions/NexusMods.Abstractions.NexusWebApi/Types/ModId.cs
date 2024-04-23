using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// An individual mod ID. Unique per game.
/// i.e. Each game has its own set of IDs and starts with 0.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct ModId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static ModId DefaultValue => From(default);
}


/// <summary>
/// Mod ID attribute, for NexusMods API mod IDs.
/// </summary>
public class ModIdAttribute(string ns, string name) 
    : ScalarAttribute<ModId, ulong>(ValueTags.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(ModId value) => value.Value;

    /// <inheritdoc />
    protected override ModId FromLowLevel(ulong value, ValueTags tags) => ModId.From(value);
} 
