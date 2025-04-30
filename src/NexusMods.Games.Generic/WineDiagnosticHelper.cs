using System.Collections.Immutable;
using System.Text;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Extensions.BCL;

namespace NexusMods.Games.Generic;

public static class WineDiagnosticHelper
{
    public static async ValueTask<string?> GetWinetricksInstructions(GameInstallation gameInstallation, ImmutableHashSet<string> requiredPackages, CancellationToken cancellationToken = default)
    {
        var locatorResultMetadata = gameInstallation.LocatorResultMetadata;
        if (locatorResultMetadata is null) return null;

        var linuxCompatibilityDataProvider = locatorResultMetadata.LinuxCompatibilityDataProvider;
        if (linuxCompatibilityDataProvider is null) return null;

        var installedPackages = await linuxCompatibilityDataProvider.GetInstalledWinetricksComponents(cancellationToken: cancellationToken);

        var missingPackages = requiredPackages.Except(installedPackages);
        if (missingPackages.Count == 0) return null;

        var sb = new StringBuilder();

        var missingPackagesString = missingPackages.Select(x => $"* `{x}`").Aggregate((a, b) => $"{a}\n{b}");

        if (locatorResultMetadata is SteamLocatorResultMetadata)
        {
            sb.AppendLine($"""
Use [protontricks](https://github.com/Matoking/protontricks) to install the following missing required packages:

{missingPackagesString}
""");
        }
        else
        {
            sb.AppendLine($"""
Use [winetricks](https://github.com/Winetricks/winetricks) to install the following missing required packages:

{missingPackagesString}
""");
        }

        return sb.ToString();
    }

    public static async ValueTask<string?> GetWineDllOverridesUpdateInstructions(GameInstallation gameInstallation, WineDllOverride[] requiredOverrides, CancellationToken cancellationToken = default)
    {
        var locatorResultMetadata = gameInstallation.LocatorResultMetadata;
        if (locatorResultMetadata is null) return null;

        var linuxCompatibilityDataProvider = locatorResultMetadata.LinuxCompatibilityDataProvider;
        if (linuxCompatibilityDataProvider is null) return null;

        var existingOverrides = await linuxCompatibilityDataProvider.GetWineDllOverrides(cancellationToken: cancellationToken);

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
        if (locatorResultMetadata is SteamLocatorResultMetadata)
        {
            sb.AppendLine($"""
* Open Steam
* Right-click the game
* Click on "Properties..."
* Open the "General" section
* Update "Launch Options" to be the following:

```
WINEDLLOVERRIDES="{dllOverridesString}" %command%
```                  
""");
        } else if (locatorResultMetadata is HeroicGOGLocatorResultMetadata)
        {
            sb.AppendLine($"""
* Open the Heroic Games Launcher
* Right-click the game
* Click on "Settings"
* Go to the "Advanced" tab
* Update the environment variables:

```
WINEDLLOVERRIDES="{dllOverridesString}"
```
""");
        }
        else
        {
            sb.AppendLine($"""
Update the `WINEDLLOVERRIDES` environment variable to be the following:

```
{dllOverridesString}
```
""");
        }
        
        return sb.ToString();
    }
}
