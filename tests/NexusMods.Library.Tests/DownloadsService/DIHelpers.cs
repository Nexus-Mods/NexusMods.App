using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.Settings;
using NexusMods.DataModel;
using NexusMods.Jobs;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using NexusMods.Settings;

namespace NexusMods.Library.Tests.DownloadsService;

public static class DIHelpers
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Setup base path for test files
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.Library.Tests-{Guid.NewGuid()}";
        var prefix = FileSystem.Shared.GetKnownPath(baseKnownPath).Combine(baseDirectory);
        
        services
            // Add logging
            .AddLogging(builder => builder.AddXUnit())
            
            // Add FileSystem support
            .AddFileSystem()
            .AddSingleton<TemporaryFileManager>(_ => new TemporaryFileManager(FileSystem.Shared, prefix))
            
            // Add JobMonitor - this provides real IJobMonitor implementation
            .AddJobMonitor()
            
            // Add HttpClient for download jobs
            .AddHttpDownloader()
            
            // Add DataModel - provides complete MnemonicDB setup including in-memory settings
            .AddDataModel()
            .AddNexusModsLibraryModels()
            .AddSettingsManager()
            .OverrideSettingsForTests<DataModelSettings>(settings => settings with
            {
                UseInMemoryDataModel = true,
            })

            // Add DownloadsService itself
            .AddSingleton<Library.DownloadsService>();
    }
}
