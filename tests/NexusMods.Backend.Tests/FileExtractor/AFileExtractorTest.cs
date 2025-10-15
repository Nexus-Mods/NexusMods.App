using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.CrossPlatform;
using NexusMods.Paths;
using NexusMods.Sdk.FileExtractor;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Settings;

namespace NexusMods.Backend.Tests.FileExtractor;

public class AFileExtractorTest
{
    private readonly IHost _host;

    public AFileExtractorTest()
    {
        _host = new HostBuilder()
            .ConfigureServices(services =>
                {
                    const KnownPath baseKnownPath = KnownPath.EntryDirectory;
                    var baseDirectory = $"NexusMods.Examples.Tests-{Guid.NewGuid()}";
                    var prefix = NexusMods.Paths.FileSystem.Shared.GetKnownPath(baseKnownPath).Combine(baseDirectory);

                    services
                        .AddJobMonitor()
                        .AddSingleton<TimeProvider>(_ => TimeProvider.System)
                        .AddFileSystem()
                        .AddSettingsManager()
                        .AddFileExtractors()
                        .AddOSInterop()
                        .AddRuntimeDependencies()
                        .AddSettings<LoggingSettings>();
                }
            ).Build();
    }

    [Before(Test)]
    public async Task SetupServices()
    {
        await _host.StartAsync();
        ServiceProvider = _host.Services;
        JobMonitor = ServiceProvider.GetRequiredService<IJobMonitor>();
        TemporaryFileManager = ServiceProvider.GetRequiredService<TemporaryFileManager>();
        FileExtractor = ServiceProvider.GetRequiredService<IFileExtractor>();
        FileSystem = ServiceProvider.GetRequiredService<IFileSystem>();
    }

    public IFileSystem FileSystem { get; set; }

    public IFileExtractor FileExtractor { get; private set; } = null!;
    public TemporaryFileManager TemporaryFileManager { get; private set; } = null!;

    public IJobMonitor JobMonitor { get; private set; } = null!;

    public IServiceProvider ServiceProvider { get; private set; } = null!;
}
