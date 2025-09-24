using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Backend;
using NexusMods.DataModel;
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
            .AddSettingsManager()
            .OverrideSettingsForTests<DataModelSettings>(settings => settings with
            {
                UseInMemoryDataModel = true,
            })
            .AddStandardGameLocators(false)
            .AddSingleton<IFileSystem, InMemoryFileSystem>()
            .AddSingleton<TemporaryFileManager>()
            .AddStubbedGameLocators()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
