using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Backend;
using NexusMods.DataModel;
using NexusMods.Games.FileHashes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Settings;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;

namespace NexusMods.StandardGameLocators.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSkippableFactSupport()
            .AddDataModel()
            .AddLoadoutAbstractions()
            .AddFileHashes()
            .AddSettingsManager()
            .OverrideSettingsForTests<DataModelSettings>(settings => settings with
            {
                UseInMemoryDataModel = true,
            })
            .AddSingleton<IConnection>(s =>
                {
                    var settingsManager = s.GetRequiredService<ISettingsManager>();
                    var settings = settingsManager.Get<DataModelSettings>();
                    var fileSystem = s.GetRequiredService<IFileSystem>();
                    var storeSettings = new DatomStoreSettings()
                    {
                        Path = settings.UseInMemoryDataModel ? null : settings.MnemonicDBPath.ToPath(fileSystem)
                    };
                    return s.GetRequiredService<IConnectionFactory>().Create(s, storeSettings);
                }
            )
            .AddStandardGameLocators(false)
            .AddSingleton<IFileSystem, InMemoryFileSystem>()
            .AddSingleton<TemporaryFileManager>()
            .AddStubbedGameLocators()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
