using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions.Implementations;
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


    public async Task<LoggingRenderer> Run(string command, params string[] args)
    {
        var renderer = new LoggingRenderer();
        var configurator = provider.GetRequiredService<CommandLineConfigurator>();
        var result = await configurator.RunAsync(args, renderer);
        result.Should().Be(0, "The command should have succeeded");
        return renderer;
    }

}
