using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.DataOutputs;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.CLI.Tests.VerbTests;

public abstract class AVerbTest
{
    // ReSharper disable InconsistentNaming
    protected static AbsolutePath Data7ZipLZMA2 => KnownFolders.EntryFolder.CombineUnchecked(@"Resources\data_7zip_lzma2.7z");
    // ReSharper restore InconsistentNaming

    private List<object> LastLog { get; set; } = new();

    protected readonly TemporaryFileManager TemporaryFileManager;
    private readonly IServiceProvider _provider;

    protected AVerbTest(TemporaryFileManager temporaryFileManager, IServiceProvider provider)
    {
        _provider = provider;
        TemporaryFileManager = temporaryFileManager;
    }

    protected async Task RunNoBanner(params string[] args)
    {
        using var scope = _provider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<LoggingRenderer>();
        LoggingRenderer.Logs.Value = new List<object>();
        var builder = scope.ServiceProvider.GetRequiredService<CommandLineConfigurator>();
        var id = await builder.MakeRoot().InvokeAsync(new[] { "--noBanner" }.Concat(args).ToArray());
        if (id != 0)
            throw new Exception($"Bad Run Result: {id}");
        LastLog = LoggingRenderer.Logs.Value!;
    }

    protected int LogSize => LastLog.Count;
    protected Table LastTable => LastLog.OfType<Table>().First();
}
