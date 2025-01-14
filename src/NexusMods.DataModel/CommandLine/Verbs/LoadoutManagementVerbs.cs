using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
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
            .AddModule("loadouts", "Commands for managing loadouts as a whole")
            .AddModule("loadout", "Commands for managing a specific loadout")
            .AddModule("loadout groups", "Commands for managing the file groups in a loadout")
            .AddModule("loadout group", "Commands for managing a specific group of files in a loadout")
            .AddModule("loadout group items", "Commands for managing the items in a group of files in a loadout")
            .AddVerb(() => Synchronize)
            .AddVerb(() => InstallMod)
            .AddVerb(() => ListLoadouts)
            .AddVerb(() => ListGroupContents)
            .AddVerb(() => ListGroups)
            .AddVerb(() => DeleteGroupItems)
            .AddVerb(() => CreateLoadout);

    [Verb("loadout synchronize", "Synchronize the loadout with the game folders, adding any changes in the game folder to the loadout and applying any new changes in the loadout to the game folder")]
    private static async Task<int> Synchronize([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to apply")] Loadout.ReadOnly loadout,
        [Injected] ISynchronizerService syncService)
    {
        await syncService.Synchronize(loadout);
        return 0;
    }

    
    [Verb("loadout install", "Installs a mod into a loadout")]
    private static async Task<int> InstallMod([Injected] IRenderer renderer,
        [Option("l", "loadout", "loadout to add the mod to")] Loadout.ReadOnly loadout,
        [Option("f", "file", "Mod file to install")] AbsolutePath file,
        [Option("n", "name", "Name of the mod after installing")] string name,
        [Injected] ILibraryService libraryService,
        [Injected] CancellationToken token)
    {
        return await renderer.WithProgress(token, async () =>
        {
            var localFile = await libraryService.AddLocalFile(file); 
            await libraryService.InstallItem(localFile.AsLibraryFile().AsLibraryItem(), loadout);
            return 0;
        });
    }


    [Verb("loadouts list", "Lists all the loadouts")]
    private static async Task<int> ListLoadouts([Injected] IRenderer renderer,
        [Injected] IConnection conn,
        [Injected] CancellationToken token)
    {
        var db = conn.Db;
        await Loadout.All(db)
            .Where(x => x.IsVisible())
            .Select(list => (list.Name, list.Installation.Name, list.LoadoutId, list.Items.Count))
            .RenderTable(renderer, "Name", "Game", "Id", "Items");
        return 0;
    }

    [Verb("loadout group list", "Lists the contents of a loadout group")]
    private static async Task<int> ListGroupContents([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to load")] Loadout.ReadOnly loadout,
        [Option("g", "group", "Name of the group to list")] string groupName,
        [Option("f", "filterFiles", "Filter files by the given glob", true)] Matcher? filterFiles,
        [Injected] CancellationToken token)
    {
        var mod = loadout.Items
            .OfTypeLoadoutItemGroup()
            .First(m => m.AsLoadoutItem().Name == groupName);
        
        if (!mod.IsValid())
            return await renderer.InputError("Group {0} not found", groupName);
        
        Func<string, bool> filter = _ => true;

        if (filterFiles != null) 
            filter = s => filterFiles.Match(s).HasMatches;
        
        await mod.Children
            .Select(c =>
                {
                    var hasPath = c.TryGetAsLoadoutItemWithTargetPath(out var withPath);
                    var hasFile = withPath.TryGetAsLoadoutFile(out var withFile);

                    if (hasPath && hasFile)
                        return (withPath.TargetPath.Item2, withPath.TargetPath.Item3, withFile.Hash.ToString());

                    if (hasPath)
                        return (withPath.TargetPath.Item2, withPath.TargetPath.Item3, "<none>");

                    return default((LocationId, RelativePath, string));

                })
            .Where(v => v != default((LocationId, RelativePath, string)))
            .Where(f => filter(f.Item2.ToString()))
            .OrderBy(v => v.Item1)
            .ThenBy(v => v.Item2)
            .RenderTable(renderer, "Folder", "File", "Hash");
        return 0;
    }

    [Verb("loadout group items delete", "Deletes items from a group that match a given pattern")]
    private static async Task<int> DeleteGroupItems(
        [Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to load")] Loadout.ReadOnly loadout,
        [Option("g", "group", "Name of the group to list")] string groupName,
        [Option("f", "filterFiles", "Filter files by the given glob")] Matcher filterFiles,
        [Injected] CancellationToken token)
    {
        var mod = loadout.Items
            .OfTypeLoadoutItemGroup()
            .First(m => m.AsLoadoutItem().Name == groupName);
        
        if (!mod.IsValid())
            return await renderer.InputError("Group {0} not found", groupName);

        var ids = mod.Children
            .OfTypeLoadoutItemWithTargetPath()
            .Where(t => filterFiles.Match(t.TargetPath.Item3.ToString()).HasMatches)
            .Select(f => f.Id)
            .ToArray();
        
        await renderer.Text("Deleting {0} items", ids.Length);
        
        using var tx = loadout.Db.Connection.BeginTransaction();
        foreach (var id in ids)
            tx.Delete(id, false);
        await tx.Commit();
        
        await renderer.Text("Complete", ids.Length);
        
        return 0;
    }

    [Verb("loadout groups list", "Lists the groups in a loadout")]
    private static async Task<int> ListGroups([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to load")] Loadout.ReadOnly loadout,
        [Injected] CancellationToken token)
    {
        await loadout.Items
            .OfTypeLoadoutItemGroup()
            .Select(mod => (mod.AsLoadoutItem().Name, mod.Children.Count))
            .RenderTable(renderer, "Name", "Items");

        return 0;
    }

    [Verb("loadout create", "Create a Loadout for a given game")]
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
}
