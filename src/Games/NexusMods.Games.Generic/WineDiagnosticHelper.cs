using System.Text;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Extensions.BCL;

namespace NexusMods.Games.Generic;

public static class WineDiagnosticHelper
{
    public static string? GetWineDllOverridesUpdateInstructions(GameInstallation gameInstallation, WineDllOverride[] requiredOverrides)
    {
        if (gameInstallation.LocatorResultMetadata is not SteamLocatorResultMetadata steamLocatorResultMetadata) return null;

        var launchOptions = steamLocatorResultMetadata.GetLaunchOptions?.Invoke();
        if (launchOptions is null) return null;

        var existingOverrides = WineParser.ParseEnvironmentVariable(launchOptions);

        var overridesToAdd = new List<WineDllOverride>();
        var overridesToUpdate = new List<(WineDllOverride From, WineDllOverride To)>();

        foreach (var requiredOverride in requiredOverrides)
        {
            if (existingOverrides.TryGetFirst(dllOverride => dllOverride.DllName.Equals(requiredOverride.DllName, StringComparison.OrdinalIgnoreCase), out var found))
            {
                if (found.OverrideTypes.SequenceEqual(requiredOverride.OverrideTypes)) continue;
                overridesToUpdate.Add((From: found, To: requiredOverride));
            }
            else
            {
                overridesToAdd.Add(requiredOverride);
            }
        }

        if (overridesToAdd.Count == 0 && overridesToUpdate.Count == 0) return null;

        var sb = new StringBuilder();

        var dllOverridesString = requiredOverrides.Select(x => x.ToString()).Aggregate((a, b) => $"{a};{b}");
        sb.AppendLine($"""
- Open Steam
- Right-click the game
- Click on "Properties..."
- Open the "General" section
- Update "Launch Options" to be the following:

```
WINEDLLOVERRIDES="{dllOverridesString}" %command%
```                  
""");
        
        return sb.ToString();
    }
}
