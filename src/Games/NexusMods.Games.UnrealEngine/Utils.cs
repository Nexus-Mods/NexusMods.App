using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Games.UnrealEngine.Models;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

internal static partial class Utils
{
    public static Dictionary<LocationId, AbsolutePath> StandardUnrealEngineLocations(
        IFileSystem fileSystem,
        GameLocatorResult installation,
        AUnrealEngineGame game)
    {
        var installationPath = installation.Path;
        var arcPath = game.ArchitectureSegmentRetriever?.Invoke(installation) 
            ?? throw new InvalidOperationException("Architecture segment retriever is not set.");
        
        var (relPathGameName, relPathPakMods) = (game.RelPathGameName, game.RelPathPakMods);
        return new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installationPath },
            {
                LocationId.AppData,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine(relPathGameName)
            },
            { Constants.GameMainLocationId, installationPath.Combine(relPathGameName) },
            { Constants.BinariesLocationId, installationPath.Combine(relPathGameName.Join($"Binaries/){arcPath}")) },
            { Constants.LuaModsLocationId, installationPath.Combine(relPathGameName.Join($"Binaries/{arcPath}/Mods")) },
            { Constants.PakModsLocationId, installationPath.Combine(relPathPakMods!.Value)},
            { Constants.LogicModsLocationId, installationPath.Combine(relPathGameName.Join($"Content/Paks/LogicMods"))},
            {
                Constants.ConfigLocationId,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine(relPathGameName.Join($"Saved/Config/Windows"))
            },
            {
                LocationId.Saves,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine(relPathGameName.Join($"Saved/SaveGames"))
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
    
    public static bool TryGetGameAddon(IGameRegistry gameRegistry, GameId gameId, out IGame? gameAddon)
    {
        gameAddon = gameRegistry.InstalledGames
            .Where(game => game.Game.GameId == gameId)
            .Select(game => game.GetGame())
            .FirstOrDefault();
        return gameAddon != null;
    }
    
    public static bool TryGetUnrealEngineGameAddon(IGameRegistry gameRegistry, GameId gameId, out IUnrealEngineGameAddon? ueGameAddon)
    {
        ueGameAddon = gameRegistry.InstalledGames
            .Where(game => game.Game.GameId == gameId)
            .Select(game => game.GetGame())
            .Cast<IUnrealEngineGameAddon>()
            .FirstOrDefault();
        return ueGameAddon != null;
    }
    
    public static Dictionary<string, LibraryFile.ReadOnly[]> GroupFilesByFileName(LibraryArchive.ReadOnly libraryArchive)
    {
        return libraryArchive.Children
            .GroupBy(x => Path.GetFileNameWithoutExtension(x.Path.FileName))
            .ToDictionary(g =>
                g.Key,
                g => g.Select(x => x.AsLibraryFile())
                    .ToArray());
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
