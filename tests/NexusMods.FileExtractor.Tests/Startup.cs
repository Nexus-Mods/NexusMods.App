using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Activities;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Activities;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddFileSystem()
            .AddActivityMonitor()
            .AddFileExtractors();
    }
}

