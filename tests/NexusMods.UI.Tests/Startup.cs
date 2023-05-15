using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App;
using NexusMods.Common;
using NexusMods.Games.RedEngine;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using NexusMods.UI.Tests.Framework;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.UI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked("temp").CombineUnchecked(Guid.NewGuid().ToString());
        path.CreateDirectory();
        var config = new AppConfig
        {
            DataModelSettings =
            {
                ArchiveLocations = new[] { new ConfigurationPath(path.CombineUnchecked("Archives"))},
                DataStoreFilePath = new ConfigurationPath(path.CombineUnchecked("DataModel.sqlite")),
                IpcDataStoreFilePath = new ConfigurationPath(path.CombineUnchecked("DataModel.IPC.sqlite"))
            }
        };
        
        services.AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
                .AddApp(addStandardGameLocators: false, config: config)
                .AddSingleton<AvaloniaApp>()
                .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}

