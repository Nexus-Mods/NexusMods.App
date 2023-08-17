using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;
using NexusMods.CLI;
using NexusMods.CLI.Tests;
using NexusMods.Common;
using NexusMods.Games.Generic;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<SkyrimSpecialEdition>(new Version("1.6.659.0"))
            .AddUniversalGameLocator<SkyrimLegendaryEdition>(new Version("1.9.32.0"))
            .AddBethesdaGameStudios()
            .AddGenericGameSupport()
            .AddCLI()
            .AddAllScoped<IRenderer, LoggingRenderer>()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
