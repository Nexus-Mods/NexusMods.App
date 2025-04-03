using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

public class UESynchronizer<TSettings> : ALoadoutSynchronizer where TSettings : class, ISettings, new()
{
    private IEnumerable<GamePath> _ignorePaths;
    private TSettings _settings;
    private GameId _gameId;

    private readonly IGameRegistry _gameRegistry;
    private readonly IFileSystem _fs;

    public UESynchronizer(IServiceProvider provider, GameId gameId) : base(provider)
    {
        _gameId = gameId;
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _gameRegistry = provider.GetRequiredService<IGameRegistry>();
        _fs = provider.GetRequiredService<IFileSystem>();
        _settings = settingsManager.Get<TSettings>();
        void AssignmentFunc(TSettings x) => _settings = x;
        settingsManager.GetChanges<TSettings>().Subscribe(AssignmentFunc);
        _ignorePaths = IgnoreFolders();
    }

    private bool IgnoreExecutables { get; set; } = false;

    private GamePath[] IgnoreFolders()
    {
        var gameInstallation = _gameRegistry.InstalledGames.FirstOrDefault(game => game.GetGame().GameId == _gameId);
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
    
    // public override async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
    // {
    //     loadout = await base.Synchronize(loadout);
    //     return await base.Synchronize(loadout);
    // }

    // public override bool IsIgnoredBackupPath(GamePath path)
    // {
    //     return false;
    //      if (IgnoreExecutables && path.Extension == Constants.ExeExt) return true;
    //      return false;
    // }

    // protected override bool ShouldIgnorePathWhenIndexing(GamePath path) =>
    //     _ignorePaths.Any(path.StartsWith) ||
    //     path.StartsWith(Constants.EnginePath) ||
    //     path.StartsWith(Constants.ResourcesPath);

    // public override bool IsIgnoredPath(GamePath path)
    // {
    //     if (IgnoreExecutables && path.Extension == Constants.ExeExt) return true;
    //     return false;
    // }
}
