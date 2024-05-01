using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

public class GameInstallationAttribute(string ns, string name) : 
    ScalarAttribute<GameInstallation, string>(ValueTags.Utf8, ns, name)
{
    protected override string ToLowLevel(GameInstallation value)
    {
        // TODO: Replace this with a reference to the actual game install in the store
        return $"{value.Game.Domain}|{value.Version}|{value.Store}";
    }

    protected override GameInstallation FromLowLevel(string value, ValueTags tag)
    {
        throw new NotImplementedException("DO this");
        return base.FromLowLevel(value, tag);
    }
}
