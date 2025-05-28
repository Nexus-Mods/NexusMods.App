using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.DataModel.CommandLine.Verbs;

internal static class ToolVerbs
{
    internal static IServiceCollection AddToolVerbs(this IServiceCollection collection) =>
        collection.AddVerb(() => ListTools)
            .AddVerb(() => RunTool);

    [Verb("list-tools", "Lists all tools available")]
    private static async Task<int> ListTools([Injected] IRenderer renderer,
        [Injected] IEnumerable<ITool> tools,
        [Injected] CancellationToken token)
    {
        await renderer.Table(new[] { "Name", "Description" },
            tools.Select(t => new object[] { t.Name, string.Join(", ", t.GameIds) }));
        return 0;
    }

    [Verb("run-tool", "Runs a tool")]
    private static async Task<int> RunTool([Injected] IRenderer renderer,
        [Option("t", "tool", "Tool to run")] ITool tool,
        [Option("l", "loadout", "Loadout to run the tool on")] LoadoutId loadout,
        [Injected] IToolManager toolManager,
        [Injected] CancellationToken token)
    {
        throw new NotImplementedException();
        /*
        await renderer.WithProgress(token, async () =>
        {
            await toolManager.RunTool(tool, loadout.Value, token:token);
            return 0;
        });
        return 0;
        */
    }
}
