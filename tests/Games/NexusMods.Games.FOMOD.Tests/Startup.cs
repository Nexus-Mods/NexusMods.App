using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.FOMOD.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddFomod()
            .AddSingleton<ICoreDelegates, MockDelegates>()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
