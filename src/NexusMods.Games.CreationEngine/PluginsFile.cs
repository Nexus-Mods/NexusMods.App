using System.Text;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Games.CreationEngine;

public class PluginsFile : IIntrinsicFile
{
    private readonly ICreationEngineGame _game;
    private readonly ISorter _sorter;
    private readonly ILogger<PluginsFile> _logger;

    private record struct Metadata(RelativePath FileName, ModKey ModKey, Hash Hash, IMod Mod);

    public PluginsFile(ILogger<PluginsFile> logger, ICreationEngineGame game, ISorter sorter)
    {
        _game = game;
        _sorter = sorter;
        Path = game.PluginsFile;
        _logger = logger;
    }
    
    public GamePath Path { get; }
    
    public async Task Write(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree)
    {
        /*
        var db = loadout.Db;
        var sortOrder = db.Connection.Query<ModKey>(
            $"""
                 SELECT ModKey FROM creation_engine.plugin_sort_order({db}) items
                 LEFT JOIN mdb_SortOrder(Db=>{db}) so ON items.SortOrderId = so.Id
                 WHERE so.ParentEntity = {loadout.Id}
                 ORDER BY items.SortIndex
                 """);
        await using var sw = new StreamWriter(stream, leaveOpen: true);
        await sw.WriteLineAsync("# File maintained by the Nexus Mods app");
        foreach (var entry in sortOrder)
        {
            await sw.WriteLineAsync("*" + entry);
        }
        */
    }
    

    public Task Ingest(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree, ITransaction tx)
    {
        return Task.CompletedTask;
        // TODO: There's no way to test this code until we have a file scanner to detect on-disk changes
        /*
        using var sr = new StreamReader(stream, Encoding.UTF8);

        Dictionary<ModKey, int> newSortOrder = new();
        while (sr.ReadLine() is { } line)
        {
            if (!line.StartsWith("*")) continue;
            
            var modKey = line[1..].Trim();
            if (!ModKey.TryFromNameAndExtension(modKey, out var parsedKey))
                continue;
            
            newSortOrder.Add(parsedKey, newSortOrder.Count);
        }
        
        var db = loadout.Db;
        var sortOrder = db.Connection.Query<(EntityId Id, ModKey Key, int SortIndex)>(
            $"""
                 SELECT Id, ModKey, SortIndex FROM creation_engine.plugin_sort_order({db}) items
                 LEFT JOIN mdb_SortOrder(Db=>{db}) so ON items.SortOrderId = so.Id
                 WHERE so.ParentEntity = {loadout.Id}
                 ORDER BY items.SortIndex
                 """);
        
        Dictionary<EntityId, (int Old, int New)> toWrite = new();

        foreach (var row in sortOrder)
        {
            if (!newSortOrder.TryGetValue(row.Key, out var newIdx)) 
                continue;
            
            toWrite[row.Id] = (row.SortIndex, newIdx);
        }

        // Any plugins that are not in the new sort order are moved to the end
        foreach (var row in sortOrder)
        {
            if (!toWrite.ContainsKey(row.Id))
                toWrite[row.Id] = (row.SortIndex, toWrite.Count);
        }

        foreach (var (id, ids) in toWrite)
        {
            if (ids.Old == ids.New)
                continue;
            
            tx.Add(id, SortOrderItem.SortIndex, ids.New);
        }
        return Task.CompletedTask;
        */
    }
}
