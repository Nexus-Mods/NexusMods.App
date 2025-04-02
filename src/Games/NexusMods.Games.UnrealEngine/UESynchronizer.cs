using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

public class UESynchronizer(IServiceProvider provider) : ALoadoutSynchronizer(provider)
{
    private readonly Dictionary<GameId, object> _settingsDictionary = new();
    private readonly Dictionary<GameId, GamePath[]> _ignoreDictionary = new();
    
    private readonly ISettingsManager _settingsManager = provider.GetRequiredService<ISettingsManager>();
    private readonly IGameRegistry _gameRegistry = provider.GetRequiredService<IGameRegistry>();
    private readonly IFileSystem _fs = provider.GetRequiredService<IFileSystem>();

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
        if (_ignoreDictionary.TryGetValue(id, out var ignorePaths))
            return ignorePaths;
        
        var gameInstallation = _gameRegistry.InstalledGames.FirstOrDefault(game => game.GetGame().GameId == id);
        var game = gameInstallation?.GetGame();
        if (game == null) return [];
        var ueGame = game as IUnrealEngineGameAddon;
        if (ueGame == null) return [];
        
        IgnoreExecutables = gameInstallation?.Store == GameStore.XboxGamePass;

        var ignoredGamePaths = IgnoreExecutables
            ? new[] { Constants.EnginePath, Constants.ResourcesPath, game.GetPrimaryFile(GameStore.XboxGamePass) }
            : [];
        return ignoredGamePaths;
    }

    public override bool IsIgnoredPath(GamePath path)
    {
        if (IgnoreExecutables && path.Extension == Constants.ExeExt) return true;
        return false;
    }
}
