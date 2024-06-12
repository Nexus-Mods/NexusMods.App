using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into a loadout marker
/// </summary>
[UsedImplicitly]
internal class LoadoutParser(IConnection conn) : IOptionParser<Loadout.ReadOnly>
{
    public bool TryParse(string input, out Loadout.ReadOnly value, out string error)
    {
        var db = conn.Db;
        error = string.Empty;
        if (LoadoutId.TryParseFromHex(input, out var parsedId))
        {
            var loadout = Loadout.Load(db, parsedId);
            if (loadout.IsValid())
            {
                value = loadout;
                return true;
            }
        }

        var found = Loadout.FindByName(db, input).ToArray();

        switch (found.Length)
        {
            case 0:
                throw new Exception($"No loadout found with name or id {input}");
            case > 1:
                throw new Exception($"Multiple loadouts found with name {input}");
            case 1:
                value = found[0];
                return true;
            default:
                throw new Exception($"No loadout found with name {input}");
        }
    }
}
