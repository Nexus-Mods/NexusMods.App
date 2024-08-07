using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Trees;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.DataModel.CommandLine.Verbs;

/// <summary>
/// Loadout management verbs for the commandline interface
/// </summary>
public static class LoadoutManagementVerbs
{
    /// <summary>
    /// Register the loadout management verbs
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddLoadoutManagementVerbs(this IServiceCollection services) =>
        services
            .AddVerb(() => Synchronize)
            .AddVerb(() => ChangeTracking)
            .AddVerb(() => Ingest)
            .AddVerb(() => InstallMod)
            .AddVerb(() => ListLoadouts)
            .AddVerb(() => ListModContents)
            .AddVerb(() => ListMods)
            .AddVerb(() => CreateLoadout)
            .AddVerb(() => RenameLoadout)
            .AddVerb(() => RemoveLoadout);

    [Verb("synchronize", "Synchronize the loadout with the game folders, adding any changes in the game folder to the loadout and applying any new changes in the loadout to the game folder")]
    private static async Task<int> Synchronize([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to apply")] Loadout.ReadOnly loadout,
        [Injected] ISynchronizerService syncService)
    {
        await syncService.Synchronize(loadout);
        return 0;
    }

    [Verb("ingest", "Ingest changes from the game folders into the given loadout")]
    private static async Task<int> Ingest([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout ingest changes into")] LoadoutId loadout)
    {
        throw new NotImplementedException();
        /*
        var state = await loadout.Value.Ingest();
        loadout.Alter("Ingest changes from the game folder", _ => state);

        await renderer.Text($"Ingested game folder changes into {loadout.Value.Name}");

        return 0;
        */
    }

    [Verb("change-tracking", "Show changes for the given loadout")]
    private static async Task<int> ChangeTracking([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to track changes for")] LoadoutId loadout,
        [Injected] CancellationToken token)
    {
        throw new NotImplementedException();
        /*
        using var d = registry.Revisions(loadout.Id)
            .Subscribe(async id =>
            {
                await renderer.Text("Revision {Id} for {LoadoutId}", id, loadout.Id);
            });

        while (!token.IsCancellationRequested)
            await Task.Delay(1000, token);
        return 0;
        */
    }
    
    [Verb("install-mod", "Installs a mod into a loadout")]
    private static async Task<int> InstallMod([Injected] IRenderer renderer,
        [Option("l", "loadout", "loadout to add the mod to")] Loadout.ReadOnly loadout,
        [Option("f", "file", "Mod file to install")] AbsolutePath file,
        [Option("n", "name", "Name of the mod after installing")] string name,
        [Injected] IArchiveInstaller archiveInstaller,
        [Injected] IFileOriginRegistry fileOriginRegistry,
        [Injected] CancellationToken token)
    {
        return await renderer.WithProgress(token, async () =>
        {
            var downloadId = await fileOriginRegistry.RegisterDownload(file, 
            (tx, id) => tx.Add(id, FilePathMetadata.OriginalName, file.FileName), name, token);

            await archiveInstaller.AddMods(loadout.LoadoutId, downloadId, name, token: token);
            return 0;
        });
    }


    [Verb("list-loadouts", "Lists all the loadouts")]
    private static async Task<int> ListLoadouts([Injected] IRenderer renderer,
        [Injected] IConnection conn,
        [Injected] CancellationToken token)
    {
        var db = conn.Db;
        var rows = Loadout.All(db)
            .Where(x => x.IsVisible())
            .Select(list => new object[] { list.Name, list.Installation, list.LoadoutId, list.Mods.Count })
            .ToList();

        await renderer.Table(["Name", "Game", "Id", "Mod Count"], rows);
        return 0;
    }

    [Verb("list-mod-contents", "Lists the contents of a mod")]
    private static async Task<int> ListModContents([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to load")] Loadout.ReadOnly loadout,
        [Option("m", "mod", "Mod to print the contents of")] string modName,
        [Injected] CancellationToken token)
    {
        var rows = new List<object[]>();
        var mod = loadout.Mods.First(m => m.Name == modName);
        foreach (var file in mod.Files)
        {
            if (file.TryGetAsStoredFile(out var stored))
                rows.Add([file.To, stored.Hash]);
            else
                rows.Add([file.GetType().ToString(), "<none>"]);
        }

        await renderer.Table(["Name", "Source"], rows);
        return 0;
    }

    [Verb("list-mods", "Lists the mods in a loadout")]
    private static async Task<int> ListMods([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to load")] Loadout.ReadOnly loadout,
        [Injected] CancellationToken token)
    {
        var rows = loadout.Items
            .OfTypeLoadoutItemGroup()
            .Where(group => !group.Contains(LoadoutItem.Parent))
            .Select(mod => new object[] { mod.AsLoadoutItem().Name })
            .ToList();

        await renderer.Table(["Name"], rows);
        return 0;
    }

    [Verb("rename", "Rename a loadout id to a specific registry name")]
    private static async Task<int> RenameLoadout([Option("l", "loadout", "Loadout to assign a name")] LoadoutId loadout,
        [Option("n", "name", "Name to assign the loadout")] string name,
        [Injected] LoadoutId registry)
    {
        throw new NotImplementedException();
        /*
        registry.Alter(loadout.LoadoutId, $"Renamed {loadout.DataStoreId} to {name}", _ => loadout);
        return 0;
        */
    }

    [Verb("create-loadout", "Create a Loadout for a given game")]
    private static async Task<int> CreateLoadout([Injected] IRenderer renderer,
        [Option("g", "game", "Game to create a loadout for")] IGame game,
        [Option("v", "version", "Version of the game to manage")] Version version,
        [Option("n", "name", "The name of the new loadout")] string name,
        [Injected] IGameRegistry registry,
        [Injected] CancellationToken token)
    {
        
        var install = registry.Installations.Values.FirstOrDefault(g => g.Game == game);
        if (install == null)
            throw new Exception("Game installation not found");

        return await renderer.WithProgress(token, async () =>
        {
            await game.Synchronizer.CreateLoadout(install, name);
            return 0;
        });
    }

    [Verb("remove-loadout", "Remove a loadout by its ID")]
    private static async Task<int> RemoveLoadout(
        [Injected] IRenderer renderer,
        [Injected] IConnection conn,
        [Option("l", "loadout", "Loadout to delete")] LoadoutId loadoutId,
        [Injected] CancellationToken token)
    {

        // TODO: make this call into the new removal logic that uses disk states
        throw new Exception("Not implemented");
    }
}
