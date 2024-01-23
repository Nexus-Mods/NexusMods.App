using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Common;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.Generic.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddGenericGameSupport()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddDataModelEntities()
            .AddDataModelBaseEntities()
            .AddInstallerTypes()
            .Validate();
    }
}
