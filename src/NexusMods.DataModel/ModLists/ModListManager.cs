﻿using System.Collections;
using System.Collections.Immutable;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.ModLists.Markers;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists;

public class ModListManager
{
    private readonly ILogger<ModListManager> _logger;
    private readonly IDataStore _store;
    private readonly Root<ListRegistry> _root;
    private readonly FileHashCache _fileHashCache;
    private readonly IModInstaller[] _installers;
    private readonly ArchiveContentsCache _analyzer;

    public ModListManager(ILogger<ModListManager> logger, IDataStore store, FileHashCache fileHashCache, IEnumerable<IModInstaller> installers, ArchiveContentsCache analyzer)
    {
        _logger = logger;
        _store = store;
        _root = new Root<ListRegistry>(RootType.ModLists, store);
        _fileHashCache = fileHashCache;
        _installers = installers.ToArray();
        _analyzer = analyzer;
    }

    public IObservable<ListRegistry> Changes => _root.Changes.Select(r => r.New);
    public IEnumerable<ModListMarker> AllModLists => _root.Value.Lists.Values.Select(m => new ModListMarker(this, m.ModListId));

    public async Task<ModListMarker> ManageGame(GameInstallation installation, string name = "", CancellationToken? token = null)
    {
        _logger.LogInformation("Indexing game files");
        var gameFiles = new HashSet<AModFile>();

        foreach (var (type, path) in installation.Locations)
        {
            await foreach (var result in _fileHashCache.IndexFolder(path, token))
            {
                gameFiles.Add(new GameFile
                {
                    To = new GamePath(type, result.Path.RelativeTo(path)),
                    Installation = installation,
                    Hash = result.Hash,
                    Size = result.Size,
                    Store = _store
                });
            }
        }
        _logger.LogInformation("Creating Modlist {Name}", name);
        var mod = new Mod
        {
            Name = "Game Files",
            Files = new EntityHashSet<AModFile>(_store, gameFiles.Select(g => g.Id)),
            Store = _store
        };
        
        var n = ModList.Empty(_store) with
        {
            Installation = installation,
            Name = name, 
            Mods = new EntityHashSet<Mod>(_store, new [] {mod.Id})
        };
        _root.Alter(r => r with {Lists = r.Lists.With(n.ModListId, n)});
        
        _logger.LogInformation("Modlist {Name} {Id} created", name, n.ModListId);
        return new ModListMarker(this, n.ModListId);
    }

    public async Task<ModListMarker> InstallMod(ModListId modListId, AbsolutePath path, CancellationToken token = default)
    {
        var modList = GetModList(modListId);
        
        var analyzed = (await _analyzer.AnalyzeFile(path, token) as AnalyzedArchive);

        var installer = _installers
            .Select(i => (Installer: i, Priority: i.Priority(modList.Value.Installation, analyzed.Contents)))
            .Where(p => p.Priority != Priority.None)
            .OrderBy(p => p.Priority)
            .FirstOrDefault();
        if (installer == default)
            throw new Exception($"No Installer found for {path}");

        var contents = installer.Installer.Install(modList.Value.Installation, analyzed.Hash, analyzed.Contents);

        var newMod = new Mod()
        {
            Name = path.FileName.ToString(),
            Files = new EntityHashSet<AModFile>(_store, contents.Select(c => c.Id)),
            Store = _store
        };
        modList.Add(newMod);
        return modList;
    }

    private ModListMarker GetModList(ModListId modListId)
    {
        return new ModListMarker(this, modListId);
    }

    public void Alter(ModListId id, Func<ModList, ModList> func)
    {
        _root.Alter(r =>
        {
            var newList = func(r.Lists[id]);
            return r with { Lists = r.Lists.With(newList.ModListId, newList) };
        });
    }

    public ModList Get(ModListId id)
    {
        return _root.Value.Lists[id];
    }
}