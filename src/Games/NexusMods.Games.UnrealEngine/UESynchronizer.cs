using System.Text;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Games.UnrealEngine.Models;
using NexusMods.Games.UnrealEngine.SortOrder;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

public class UESynchronizer<TSettings> : ALoadoutSynchronizer where TSettings : class, ISettings, new()
{
    private IEnumerable<GamePath> _ignorePaths;
    private TSettings _settings;
    private GameId _gameId;

    private readonly IGameRegistry _gameRegistry;
    private readonly IFileSystem _fs;
    private readonly IFileStore _fileStore;
    private readonly TemporaryFileManager _tfm;

    public UESynchronizer(IServiceProvider provider, GameId gameId) : base(provider)
    {
        _gameId = gameId;
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _gameRegistry = provider.GetRequiredService<IGameRegistry>();
        _fs = provider.GetRequiredService<IFileSystem>();
        _fileStore = provider.GetRequiredService<IFileStore>();
        _tfm = provider.GetRequiredService<TemporaryFileManager>();
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
    
    public override async Task<Loadout.ReadOnly> Synchronize(Loadout.ReadOnly loadout)
    {
        loadout = await base.Synchronize(loadout);
        var luaMods = ScriptingSystemLuaLoadoutItem.All(loadout.Db)
            .Where(l => l.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId)
            .ToArray();
        
        if (!luaMods.Any() || !Utils.TryGetLuaModsLoadOrderFile(loadout, out var loadOrderFiles)) 
            return loadout;
        
        var serializeableLuaMods = luaMods
            .Select(x => new LuaJsonEntry()
            {
                ModName = x.LoadOrderName,
                ModEnabled = !x.AsLoadoutItemGroup().AsLoadoutItem().IsDisabled,
            })
            .ToArray();
        
        var deserializedData = loadOrderFiles!
            .SelectMany(loFile =>
            {
                var fileName = loFile.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName;
                using var stream = _fileStore.GetFileStream(loFile.Hash, CancellationToken.None).Result;
                using var sr = new StreamReader(stream);
                var data = sr.ReadToEnd();
                if (data == string.Empty) return Array.Empty<LuaJsonEntry>();
                var deserializedEntries = fileName.Extension.ToString() switch
                {
                    Constants.JsonExtValue => JsonConvert.DeserializeObject<LuaJsonEntry[]>(data) ?? Array.Empty<LuaJsonEntry>(),
                    Constants.TxtExtValue => data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            var parts = x.Split(':');
                            if (parts.Length < 2) return null;
                            return new LuaJsonEntry
                            {
                                ModName = parts[0].Trim(),
                                ModEnabled = parts[1].Trim() == "1",
                            };
                        })
                        .Where(entry => entry != null),
                    _ => throw new NotSupportedException($"Unsupported file extension: {fileName.Extension}"),
                };

                return serializeableLuaMods
                    .Concat(deserializedEntries)
                    .GroupBy(entry => entry!.ModName)
                    .Select(group => group.First())
                    .ToArray();
            })
            .GroupBy(entry => entry!.ModName)
            .Select(group => group.First())
            .ToList();
        
        var serializedData = JsonConvert.SerializeObject(deserializedData, Formatting.Indented);
        await _fs.WriteAllTextAsync(
            Constants.LuaModsLoadOrderFileJson.CombineChecked(loadout.InstallationInstance),
            serializedData);
        
        return await base.Synchronize(loadout);
    }

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
