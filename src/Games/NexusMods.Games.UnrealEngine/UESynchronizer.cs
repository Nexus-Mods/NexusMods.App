using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;
/*
 * Ignore this for now - still WIP
 */
public class UESynchronizer : ALoadoutSynchronizer
{
    private readonly Dictionary<GameId, object> _settingsDictionary = new();
    private readonly Dictionary<GameId, GamePath[]> _ignoreDictionary = new();
    
    private readonly ISettingsManager _settingsManager;
    private readonly IGameRegistry _gameRegistry;
    private readonly IFileSystem _fs;

    public UESynchronizer(IServiceProvider provider) : base(provider)
    {
        _settingsManager = provider.GetRequiredService<ISettingsManager>();
        _gameRegistry = provider.GetRequiredService<IGameRegistry>();
        _fs = provider.GetRequiredService<IFileSystem>();
    }

    private bool IgnoreExecutables { get; set; } = false;
    
    public void InitializeSettings<T>(GameId id) where T : class, ISettings, new()
    {
        _settingsDictionary[id] = _settingsManager.Get<T>();
        void AssignmentFunc(ISettings x) => _settingsDictionary[id] = x;
        _ignoreDictionary[id] = IgnoreFolders(id);
        _settingsManager.GetChanges<T>().Subscribe(AssignmentFunc);
    }

    private GamePath[] IgnoreFolders(GameId id)
    {
        var gameInstallation = _gameRegistry.InstalledGames.FirstOrDefault(game => game.GetGame().GameId == id);
        var game = gameInstallation?.GetGame();
        var ueGame = game as IUnrealEngineGameAddon;
        IgnoreExecutables = gameInstallation?.Store == GameStore.XboxGamePass;
        if (ueGame == null) return [];

        var ignoredGamePaths = IgnoreExecutables
            ? new[] { Constants.EnginePath, Constants.ResourcesPath }
            : [];
        return ignoredGamePaths?.ToArray() ?? [];
    }

    public override bool IsIgnoredPath(GamePath path)
    {
        if (IgnoreExecutables && path.Extension == Constants.ExeExt) return true;
        foreach (var installation in _gameRegistry.InstalledGames)
        {
            var locations = installation.LocationsRegister.GetTopLevelLocations();
            var isIgnored = locations.Any(kvp => path.LocationId == kvp.Key && path.InFolder(new GamePath(kvp.Key, kvp.Value.GetNonRootPart())));
            if (isIgnored) return true;
        }

        return false;
    }
}
