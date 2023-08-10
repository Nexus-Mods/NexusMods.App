using System.CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.App.CLI.Renderers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.CLI.Tests.VerbTests;

public class AVerbTest
{
    // ReSharper disable InconsistentNaming
    internal AbsolutePath Data7ZipLZMA2 =>
        FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_7zip_lzma2.7z");
    // ReSharper restore InconsistentNaming

    private List<object> LastLog { get; set; } = new();

    internal readonly TemporaryFileManager TemporaryFileManager;
    private readonly IServiceProvider _provider;

    internal readonly IFileSystem FileSystem;
    private readonly ILogger<AVerbTest> _logger;

    public AVerbTest(TemporaryFileManager temporaryFileManager, IServiceProvider provider)
    {
        _logger = _provider.GetRequiredService<ILogger<AVerbTest>>();
        _provider = provider;
        TemporaryFileManager = temporaryFileManager;
        FileSystem = provider.GetRequiredService<IFileSystem>();
    }

    // I added this for testing purposes to help diagnose errors easier when needed - Sewer
    [PublicAPI]
    internal void RunNoBanner(params string[] args)
    {
        using var scope = RunNoBannerInit(out var builder);
        var id = builder.MakeRoot().Invoke(new[] { "--noBanner" }.Concat(args).ToArray());
        RunNoBannerFinish(id);
    }

    public async Task RunNoBannerAsync(params string[] args)
    {
        using var scope = RunNoBannerInit(out var builder);
        var id = await builder.MakeRoot().InvokeAsync(new[] { "--noBanner" }.Concat(args).ToArray());
        RunNoBannerFinish(id);
    }

    private void RunNoBannerFinish(int id)
    {
        if (id != 0)
        {
            var spectre = new App.CLI.Renderers.Spectre(Array.Empty<IResource>());
            foreach (var item in LoggingRenderer.Logs.Value!)
            {
                Task.Run(async () => { await spectre.Render(item);}).Wait();
            }

            throw new Exception($"Bad Run Result: {id}");
        }

    }

    private IServiceScope RunNoBannerInit(out CommandLineConfigurator configurator)
    {
        var scope = _provider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<LoggingRenderer>();
        LoggingRenderer.Logs.Value = new List<object>();
        configurator = scope.ServiceProvider.GetRequiredService<CommandLineConfigurator>();
        ;
        return scope;
    }

    internal int LogSize => LastLog.Count;
    public Table LastTable => LastLog.OfType<Table>().First();
}
