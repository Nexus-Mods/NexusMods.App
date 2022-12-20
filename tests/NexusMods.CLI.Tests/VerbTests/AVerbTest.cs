using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.DataOutputs;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests.VerbTests;

public abstract class AVerbTest
{
    public static AbsolutePath Data7ZipLZMA2 => KnownFolders.EntryFolder.Combine(@"Resources\data_7zip_lzma2.7z");
    public static AbsolutePath DataZipLZMA => KnownFolders.EntryFolder.Combine(@"Resources\data_zip_lzma.zip");

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
        var logger = scope.ServiceProvider.GetRequiredService<LoggingRenderer>();
        LoggingRenderer.Logs.Value = new List<object>();
        var builder = scope.ServiceProvider.GetRequiredService<CommandLineConfigurator>();
        var id = await builder.MakeRoot().InvokeAsync(new[] { "--noBanner" }.Concat(args).ToArray());
        if (id != 0)
            throw new Exception($"Bad Run Result: {id}");
        LastLog = LoggingRenderer.Logs.Value!;
    }

    public List<object> LastLog { get; set; }

    protected int LogSize => LastLog.Count;
    protected Table LastTable => LastLog.OfType<Table>().First();
}