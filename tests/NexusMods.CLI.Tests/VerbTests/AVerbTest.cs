using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.DataOutputs;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.CLI.Tests.VerbTests;

public abstract class AVerbTest
{
    // ReSharper disable InconsistentNaming
    protected AbsolutePath Data7ZipLZMA2 => FileSystem.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked(@"Resources\data_7zip_lzma2.7z");
    // ReSharper restore InconsistentNaming

    private List<object> LastLog { get; set; } = new();

    protected readonly TemporaryFileManager TemporaryFileManager;
    private readonly IServiceProvider _provider;

    protected readonly IFileSystem FileSystem;

    protected AVerbTest(TemporaryFileManager temporaryFileManager, IServiceProvider provider)
    {
        _provider = provider;
        TemporaryFileManager = temporaryFileManager;
        FileSystem = provider.GetRequiredService<IFileSystem>();
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
