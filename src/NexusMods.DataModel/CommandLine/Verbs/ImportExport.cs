using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.DataModel.CommandLine.Verbs;

/// <summary>
/// Routines for importing and exporting the data model
/// </summary>
public static class GeneralVerbs
{
    /// <summary>
    /// Register the import/export verbs
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddImportExportVerbs(this IServiceCollection services) =>
        services
            .AddVerb(() => Ui)
            .AddModule("datamodel", "verbs that operate on the data model");
    

    [Verb("datamodel ui", "Start the DuckDB UI connected to the data model")]
    private static async Task<int> Ui(
        [Injected] IRenderer renderer,
        [Injected] IConnection connection)
    {
        try
        {
            connection.DuckDBQueryEngine.ExecuteNoPepare("INSTALL ui; LOAD ui; CALL start_ui();");
        }
        catch (Exception e)
        {
            await renderer.RenderAsync(Renderable.Text($"Error starting UI: {e.Message}"));
            return 1;
        }
        return 0;
    }

}
