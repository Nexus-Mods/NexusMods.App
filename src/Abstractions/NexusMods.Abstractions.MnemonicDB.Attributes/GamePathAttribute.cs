using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Defines a GamePath attribute.
/// </summary>
public class GamePathAttribute(string ns, string name) : ScalarAttribute<GamePath, string>(ValueTags.Utf8, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(GamePath value)
    {
        // TODO: make this a reference or something
        return $"{value.LocationId.Value}|{value.Path}";
    }

    /// <inheritdoc />
    protected override GamePath FromLowLevel(string value, ValueTags tags)
    {
        var parts = value.Split('|');
        return new GamePath(LocationId.From(parts[0]), RelativePath.FromUnsanitizedInput(parts[1]));
    }
}
