using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Abstractions.Settings;
using NexusMods.App;
using NexusMods.Paths;

namespace NexusMods.DataModel.SchemaVersions.Tests;

public class Startup
{
    /// <summary>
    /// Why are Cyberpunk tests in a generic DataModel project, well it's, so we can test something that's close to real-world data. 
    /// </summary>
    /// <param name="container"></param>
    public void ConfigureServices(IServiceCollection container)
    {
        var mode = new StartupMode { RunAsMain = true };
        
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.UI.Tests.Tests-{Guid.NewGuid()}";
        
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("temp").Combine(Guid.NewGuid().ToString());
        path.CreateDirectory();

        container
            .AddApp(startupMode: mode)
            .OverrideSettingsForTests<DataModelSettings>(settings => settings with
                {
                    UseInMemoryDataModel = true,
                    MnemonicDBPath = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/MnemonicDB.rocksdb"),
                    ArchiveLocations =
                    [
                        new ConfigurablePath(baseKnownPath, $"{baseDirectory}/Archives"),
                    ],
                }
            );
    }
}

