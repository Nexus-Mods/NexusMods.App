using System.Collections.Immutable;
using System.Text;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Extensions.BCL;

namespace NexusMods.Games.Generic;

public static class WineDiagnosticHelper
{
    public static string? GetWinetricksInstructions(GameInstallation gameInstallation, ImmutableHashSet<string> requiredPackages)
    {
        // TODO: support more than Steam
        if (gameInstallation.LocatorResultMetadata is not SteamLocatorResultMetadata steamLocatorResultMetadata) return null;

        var protonPrefixDirectory = steamLocatorResultMetadata.ProtonPrefixDirectory;
        if (protonPrefixDirectory is null) return null;

        var winePrefixDirectory = protonPrefixDirectory.Value.Combine("pfx");

        // https://github.com/Winetricks/winetricks/blob/e73c4d8f71801fe842c0276b603d9c8024d6d957/src/winetricks#L4216-L4225
        var winetricksFilePath = winePrefixDirectory.Combine("winetricks.log");
        var installedPackages = WineParser.ParseWinetricksLogFile(winetricksFilePath);

        var missingPackages = requiredPackages.Except(installedPackages);
        if (missingPackages.Count == 0) return null;

        var sb = new StringBuilder();

        var missingPackagesString = missingPackages.Select(x => $"- `{x}`").Aggregate((a, b) => $"{a}\n{b}");

        sb.AppendLine($"""
Use [protontricks](https://github.com/Matoking/protontricks) to install the following missing required packages:

{missingPackagesString}
""");

        return sb.ToString();
    }

    public static string? GetWineDllOverridesUpdateInstructions(GameInstallation gameInstallation, WineDllOverride[] requiredOverrides)
    {
        // TODO: support more than Steam
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
