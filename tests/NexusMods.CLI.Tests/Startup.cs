using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.CLI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddStandardGameLocators(false);
        container.AddStubbedGameLocators();
        container.AddCLI();
        container.AddAllScoped<IRenderer, LoggingRenderer>();
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
    }
}

