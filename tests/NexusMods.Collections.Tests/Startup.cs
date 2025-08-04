using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary;
using Xunit.DependencyInjection;

namespace NexusMods.Collections.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddSkippableFactSupport();
    }
}

