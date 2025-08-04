using System.Diagnostics;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.App.Commandline;


/// <summary>
/// These verbs are used for checking the status of the Nexus Mods App.
/// </summary>
public static class StatusVerbs
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    internal static IServiceCollection AddStatusVerbs(this IServiceCollection services) =>
        services.AddVerb(() => Heartbeat);

    /// <summary>
    /// Returns the processId and the uptime of the Nexus Mods app process.
    /// </summary>
    /// <param name="renderer"></param>
    /// <returns></returns>
    [Verb("heartbeat", "Returns process uptime for the Nexus Mods app.")]
    private static async Task<int> Heartbeat([Injected] IRenderer renderer)
    {
        var process = Process.GetCurrentProcess();
        
        var startTime = process.StartTime;
        var uptime = DateTime.Now - startTime;
        
        await renderer.TextLine($"ProcessId: {process.Id} Uptime: {uptime.Humanize()}");

        return 0;
    }
}
