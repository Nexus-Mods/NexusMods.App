using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.UnrealEngine.Models;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

internal static partial class Utils
{
    public static Dictionary<LocationId, AbsolutePath> StandardUnrealEngineLocations(
        IFileSystem fileSystem,
        GameLocatorResult installation,
        string gameFolderName)
    {
        var installationPath = installation.Path;
        var isXbox = installation.Store == GameStore.XboxGamePass;
        var arcPath = isXbox ? "WinGDK" : "Win64";

        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installationPath },
            {
                LocationId.AppData,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine(gameFolderName)
            },
            { Constants.GameMainLocationId, installationPath.Combine(gameFolderName) },
            { Constants.BinariesLocationId, installationPath.Combine($"{gameFolderName}/Binaries/{arcPath}") },
            { Constants.LuaModsLocationId, installationPath.Combine($"{gameFolderName}/Binaries/{arcPath}/Mods") },
            { Constants.PakModsLocationId, installationPath.Combine($"{gameFolderName}/Content/Paks/~mods") },
            { Constants.LogicModsLocationId, installationPath.Combine($"{gameFolderName}/Content/Paks/LogicMods")},
            {
                Constants.ConfigLocationId,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine($"{gameFolderName}/Saved/Config/Windows")
            },
            {
                LocationId.Saves,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine($"{gameFolderName}/Saved/SaveGames")
            },
        };
    }

    public static bool TryGetScriptingSystemLoadoutGroup(Loadout.ReadOnly loadout, bool enabledOnly, out ScriptingSystemLoadoutItemGroup.ReadOnly[] ue4ss)
    {
        ue4ss = loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeScriptingSystemLoadoutItemGroup()
            .Where(group => !enabledOnly || !group.AsLoadoutItemGroup().AsLoadoutItem().IsDisabled)
            .ToArray();
        
        return ue4ss.Length > 0;
    }
    
    public static bool TryGetUnrealEngineLoadoutItems(Loadout.ReadOnly loadout, bool enabledOnly, out UnrealEngineLoadoutItem.ReadOnly[] items)
    {
        var result = new List<UnrealEngineLoadoutItem.ReadOnly>();
        foreach (var item in loadout.Items)
        {
            if (enabledOnly && item.IsDisabled) continue;
            if (item.Contains(UnrealEngineLoadoutItem.PakMetadata) &&
                item.TryGetAsLoadoutItemGroup(out var group) &&
                group.TryGetAsUnrealEngineLoadoutItem(out var ueGroup))
            {
                result.Add(ueGroup);
            }
        }
        items = result.ToArray();
        return items.Length > 0;
    }

    public static LoadoutFile.ReadOnly[] GetAllLoadoutFilesWithExt(
        Loadout.ReadOnly loadout,
        IEnumerable<LocationId> locationIds,
        IEnumerable<Extension> exts,
        bool onlyEnabledMods)
    {
        return loadout.Items
            .OfTypeLoadoutItemGroup()
            .Where(group => !onlyEnabledMods || !group.AsLoadoutItem().IsDisabled)
            .SelectMany(group => group.Children.OfTypeLoadoutItemWithTargetPath()
                .OfTypeLoadoutFile()
                .Where(file => locationIds.Contains(file.AsLoadoutItemWithTargetPath().TargetPath.Item2) &&
                               exts.Contains(file.AsLoadoutItemWithTargetPath().TargetPath.Item3.Extension)
                )
            )
            .ToArray();
    }
    
    public static string IndexToPrefix(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");

        var prefix = new char[3];
        for (var i = 2; i >= 0; i--)
        {
            prefix[i] = (char)('A' + (index % 26));
            index /= 26;
        }

        return new string(prefix);
    }

    public static int PrefixToIndex(string prefix)
    {
        if (prefix == null) throw new ArgumentNullException(nameof(prefix));
        if (prefix.Length != 3) throw new ArgumentException("Prefix must be exactly 3 characters long.", nameof(prefix));

        var index = 0;
        for (var i = 0; i < 3; i++)
        {
            if (prefix[i] < 'A' || prefix[i] > 'Z') throw new ArgumentException("Prefix must only contain uppercase letters A-Z.", nameof(prefix));
            index = index * 26 + (prefix[i] - 'A');
        }
        
        return index;
    }
}
