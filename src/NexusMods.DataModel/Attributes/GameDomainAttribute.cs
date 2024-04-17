using NexusMods.Abstractions.Games.DTO;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.Attributes;

/// <summary>
/// MnemonicDB attribute for the GameDomain type.
/// </summary>
public class GameDomainAttribute(string ns, string name) : ScalarAttribute<GameDomain, string>(ValueTags.Ascii, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(GameDomain value)
    {
        return value.Value;
    }

    /// <inheritdoc />
    protected override GameDomain FromLowLevel(string value, ValueTags tag)
    {
        return GameDomain.From(value);
    }
}
