using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Extensions;
using NexusMods.DataModel.Loadouts.Markers;
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
            .AddVerb(() => Apply)
            .AddVerb(() => ChangeTracking)
            .AddVerb(() => Ingest);

    [Verb("apply", "Apply the given loadout to the game folder")]
    private static async Task<int> Apply([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to apply")]
        LoadoutMarker loadout)
    {
        var state = await loadout.Value.Apply();

        var summary = state.GetAllDescendentFiles()
            .Aggregate((Count:0, Size:Size.Zero), (acc, file) => (acc.Item1 + 1, acc.Item2 + file.Value.Size));

        await renderer.Text($"Applied {loadout} resulting state contains {summary.Count} files and {summary.Size} of data");

        return 0;
    }

    [Verb("ingest", "Ingest changes from the game folders into the given loadout")]
    private static async Task<int> Ingest([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout ingest changes into")] LoadoutMarker loadout)
    {
        var state = await loadout.Value.Ingest();
        loadout.Alter("Ingest changes from the game folder", _ => state);

        await renderer.Text($"Ingested game folder changes into {loadout.Value.Name}");

        return 0;
    }

    [Verb("change-tracking", "Show changes for the given loadout")]
    private static async Task<int> ChangeTracking([Injected] IRenderer renderer,
        [Option("l", "loadout", "Loadout to track changes for")] LoadoutMarker loadout,
        [Injected] LoadoutRegistry registry,
        [Injected] CancellationToken token)
    {
        using var d = registry.Revisions(loadout.Id)
            .Subscribe(async id =>
            {
                await renderer.Text("Revision {Id} for {LoadoutId}", id, loadout.Id);
            });

        while (!token.IsCancellationRequested)
            await Task.Delay(1000, token);
        return 0;
    }

}
