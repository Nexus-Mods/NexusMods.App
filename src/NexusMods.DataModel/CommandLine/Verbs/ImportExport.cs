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
public static class ImportExport
{
    /// <summary>
    /// Register the import/export verbs
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddImportExportVerbs(this IServiceCollection services) =>
        services.AddVerb(() => Export);
    
    [Verb("export-datamodel", "Export the data model to a file")]
    private static async Task<int> Export([Injected] IRenderer renderer, 
        [Injected] IConnection connection,
        [Option("o", "output", "Output file, the contents will be compressed with deflate")] AbsolutePath output)
    {
        await using var stream = output.Create();
        await renderer.RenderAsync(Renderable.Text("Exporting data model to file"));
        await using var deflateStream = new DeflateStream(stream, CompressionLevel.Optimal); 
        await connection.DatomStore.ExportAsync(deflateStream);
        await renderer.RenderAsync(Renderable.Text("Exported data model to file"));
        return 0;
    }

}
