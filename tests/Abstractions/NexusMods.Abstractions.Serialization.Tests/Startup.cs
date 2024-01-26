using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using Xunit.DependencyInjection;

namespace NexusMods.Abstractions.Serialization.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSkippableFactSupport()
            .AddDataModelBaseEntities()
            .AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Startup).Assembly))
            .AddLogging(builder => builder.AddXUnit());
    }
}

