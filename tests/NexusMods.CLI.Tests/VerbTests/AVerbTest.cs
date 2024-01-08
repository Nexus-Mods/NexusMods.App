using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using NexusMods.SingleProcess;

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
        var result = await configurator.RunAsync(args, renderer, CancellationToken.None);
        if (result != 0)
        {
            var errorLog = renderer.Logs.OfType<Text>().Select(t => t.Template).Aggregate((acc, itm) => acc + itm);
            throw new Exception($"The command should have succeeded instead got: \n\n {errorLog}");
        }

        return renderer;
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
