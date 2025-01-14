using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using NexusMods.SingleProcess;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using Spectre.Console.Testing;

namespace NexusMods.CLI.Tests.VerbTests;

public class AVerbTest(IServiceProvider provider)
{
    internal TemporaryFileManager TemporaryFileManager => provider.GetRequiredService<TemporaryFileManager>();

    // ReSharper disable InconsistentNaming
    internal AbsolutePath Data7ZipLZMA2 =>
        FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/data_7zip_lzma2.7z");
    // ReSharper restore InconsistentNaming


    internal readonly IFileSystem FileSystem = ServiceProviderServiceExtensions.GetRequiredService<IFileSystem>(provider);


    /// <summary>
    /// Runs a CLI command as if the program was invoked with the given arguments. The output is captured and returned.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<LoggingRenderer> Run(params string[] args)
    {
        var renderer = new LoggingRenderer();
        var configurator = provider.GetRequiredService<CommandLineConfigurator>();
        var withSplitName = args[0].Split(' ').Concat(args[1..]).ToArray();
        var result = await configurator.RunAsync(withSplitName, renderer, CancellationToken.None);
        if (result != 0)
        {
            var errorLog = renderer.Logs.OfType<Text>().Select(t => t.Template).Aggregate((acc, itm) => acc + itm);
            throw new Exception($"The command should have succeeded instead got: \n\n {errorLog}");
        }

        return renderer;
    }

    /// <summary>
    /// Verifies the last table in the given renderer against the given name.
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="name"></param>
    protected async Task VerifyTable(LoggingRenderer renderer, string name, [CallerMemberName] string? caller = null)
    {
        var last = renderer.Logs.Last();
        if (last is not Table table)
            throw new Exception("No table was rendered");
        
        caller ??= "unknown";
        var console = new TestConsole();
        console.Profile.Width = 120;
        var proxyRenderer = new SpectreRenderer(console);
        
        await proxyRenderer.RenderAsync(table);

        await Verify(console.Output).UseFileName($"{name}_{caller}_table.txt");
    }
    
    /// <summary>
    /// Verifies the log output of the given renderer against the given name.
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="name"></param>
    protected async Task VerifyLog(LoggingRenderer renderer, string name, [CallerMemberName] string? caller = null)
    {
        caller ??= "unknown";
        var console = new TestConsole();
        console.Profile.Width = 120;
        var proxyRenderer = new SpectreRenderer(console);
        
        foreach (var item in renderer.Logs)
        {
            await proxyRenderer.RenderAsync(item);
        }
        await Verify(console.Output).UseFileName($"{name}_{caller}.txt");
    }

    /// <summary>
    /// Creates a new stubbed game installation, registers it with the game registry, and returns it.
    /// </summary>
    /// <returns></returns>
    public async Task<GameInstallation> CreateInstall()
    {
        return await StubbedGame.Create(provider);
    }

    /// <summary>
    /// Runs the given verb with the given arguments. No conversion is done on the arguments, so they must be the correct
    /// type as defined in the verb definition.
    /// </summary>
    /// <param name="verbName"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public async Task<int> RunDirectly(string verbName, params object[] args)
    {
        var verbDefinition = provider.GetServices<VerbDefinition>()
            .FirstOrDefault(verb => verb.Name == verbName);

        return await (Task<int>)verbDefinition!.Info.Invoke(null, args)!;
    }

}
