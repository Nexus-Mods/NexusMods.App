using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// An attribute that contains the name of a game store.
/// </summary>
public class GameStoreAttribute(string ns, string name) : ScalarAttribute<GameStore, string>(ValueTags.Ascii, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(GameStore value)
    {
        return value.Value;
    }

    /// <inheritdoc />
    protected override GameStore FromLowLevel(string value, ValueTags tag, AttributeResolver resolver)
    {
        return GameStore.From(value);
    }
}
