using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.App;
using NexusMods.DataModel;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.StandardGameLocators.TestHelpers;
using NexusMods.UI.Tests.Framework;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.UI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.UI.Tests.Tests-{Guid.NewGuid()}";
        
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("temp").Combine(Guid.NewGuid().ToString());
        path.CreateDirectory();

        services.AddUniversalGameLocator<Cyberpunk2077Game>(new Version("1.61"))
                .AddApp(startupMode: new StartupMode()
                {
                    ShowUI = false,
                    ExecuteCli = false,
                    RunAsMain = true,
                })
                .OverrideSettingsForTests<DataModelSettings>(settings => settings with
                {
                    UseInMemoryDataModel = true,
                    MnemonicDBPath = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/MnemonicDB.rocksdb"),
                    ArchiveLocations = [
                        new ConfigurablePath(baseKnownPath, $"{baseDirectory}/Archives"),
                    ],
                })
                .AddStubbedGameLocators()
                .AddSingleton<AvaloniaApp>()
                .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
                .Validate();
    }
}

