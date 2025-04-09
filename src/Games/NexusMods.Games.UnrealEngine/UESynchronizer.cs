using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
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

        if (luaMods.Length == 0 || !Utils.TryGetLuaModsLoadOrderFile(loadout, out var loadOrderFiles))
            return loadout;

        var modStates = new Dictionary<string, LuaJsonEntry>(StringComparer.OrdinalIgnoreCase);

        // Add the user's lua mods first.
        foreach (var mod in luaMods)
        {
            var item = mod.AsLoadoutItemGroup().AsLoadoutItem();
            modStates[mod.LoadOrderName] = new LuaJsonEntry
            {
                ModName = mod.LoadOrderName,
                ModEnabled = !item.IsDisabled,
            };
        }

        // Merge with existing file data
        foreach (var loFile in loadOrderFiles!)
        {
            var fileName = loFile.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName;
            var ext = fileName.Extension.ToString();

            await using var stream = await _fileStore.GetFileStream(loFile.Hash, CancellationToken.None);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(content))
                continue;

            var entries = ext switch
            {
                Constants.JsonExtValue => JsonConvert.DeserializeObject<LuaJsonEntry[]>(content) ?? [],
                Constants.TxtExtValue => ParseTxtFormat(content),
                _ => throw new NotSupportedException($"Unsupported file extension: {ext}"),
            };

            foreach (var entry in entries)
            {
                modStates.TryAdd(entry.ModName, entry);
            }
        }

        // Write updated files
        foreach (var file in loadOrderFiles)
        {
            var fileName = file.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName;
            var ext = fileName.Extension.ToString();

            var targetPath = ext.Equals(Constants.JsonExtValue)
                ? Constants.LuaModsLoadOrderFileJson
                : Constants.LuaModsLoadOrderFileTxt;

            var output = ext switch
            {
                Constants.JsonExtValue => JsonConvert.SerializeObject(modStates.Values, Formatting.Indented),
                Constants.TxtExtValue => string.Join(Environment.NewLine,
                    modStates.Values.Select(e => $"{e.ModName} : {(e.ModEnabled ? "1" : "0")}")
                ),
                _ => throw new NotSupportedException($"Unsupported file extension: {ext}"),
            };

            await _fs.WriteAllTextAsync(targetPath.CombineChecked(loadout.InstallationInstance), output);
        }

        return await base.Synchronize(loadout);
    }

    private static IEnumerable<LuaJsonEntry> ParseTxtFormat(string content)
    {
        return content
            .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
                {
                    var parts = line.Split(':');
                    if (parts.Length < 2) return null;
                    return new LuaJsonEntry
                    {
                        ModName = parts[0].Trim(),
                        ModEnabled = parts[1].Trim() == "1",
                    };
                }
            )
            .Where(entry => entry != null)!;
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
