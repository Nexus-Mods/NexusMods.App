using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Paths;
using NexusMods.Settings;
namespace NexusMods.App.GarbageCollection.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.DataModel.Tests-{Guid.NewGuid()}";
        services.AddFileSystem()
                .AddSettingsManager() // DataModel depends on this.
                .AddDataModel()
                .AddFileExtractors()
                .OverrideSettingsForTests<DataModelSettings>(settings => settings with
                {
                    UseInMemoryDataModel = true,
                    MnemonicDBPath = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/MnemonicDB.rocksdb"),
                    ArchiveLocations = [
                        new ConfigurablePath(baseKnownPath, $"{baseDirectory}/Archives"),
                    ],
                });
    }
}
