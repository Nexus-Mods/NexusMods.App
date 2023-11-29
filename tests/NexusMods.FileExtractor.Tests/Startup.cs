using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Activities;
using NexusMods.Common;
using NexusMods.DataModel.Activities;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddFileSystem()
            .AddAllSingleton<IActivityFactory, IActivityMonitor, ActivityMonitor>()
            .AddFileExtractors();
    }
}

