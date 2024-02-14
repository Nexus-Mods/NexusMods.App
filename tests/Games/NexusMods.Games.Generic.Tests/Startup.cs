﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.BuildInfo;
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
            .AddSerializationAbstractions()
            .AddInstallerTypes()
            .Validate();
    }
}
