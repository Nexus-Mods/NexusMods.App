using System.Globalization;
using JetBrains.Annotations;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into a loadout marker
/// </summary>
[UsedImplicitly]
internal class LoadoutParser(IConnection conn, IOptionParser<IGame> gameParser) : IOptionParser<Loadout.ReadOnly>
{
    public bool TryParse(string input, out Loadout.ReadOnly value, out string error)
    {
        var db = conn.Db;
        error = string.Empty;
        if (EntityId.TryParseFromHex(input, out var parsedId))
        {
            var loadout = Loadout.Load(db, parsedId);
            if (loadout.IsValid())
            {
                value = loadout;
                return true;
            }
        }
        
        // An id in the format of: LoadoutId:00000000000000000000000000
        if (input.StartsWith("LoadoutId:") && input.Length == 25)
        {
            var loadout = Loadout.Load(db, EntityId.From(ulong.Parse(input[10..], NumberStyles.HexNumber)));
            if (loadout.IsValid())
            {
                value = loadout;
                return true;
            }
        }
        
        // In the format of "<Game>/<ShortName>"
        if (input.Contains("/"))
        {
            var parts = input.Split('/');
            var game = parts[0];
            var shortName = parts[1];

            if (gameParser.TryParse(game, out var gameValue, out _))
            {
                if (Loadout
                    .FindByShortName(db, shortName)
                    .TryGetFirst(l => l.Installation.GameId == gameValue.GameId, out var foundLoadout))
                {
                    value = foundLoadout;
                    return true;
                }
                    
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
