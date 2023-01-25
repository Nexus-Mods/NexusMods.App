﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddHttpDownloader();
        container.AddSingleton<TemporaryFileManager>();
        container.AddSingleton<HttpClient>();
        container.AddSingleton<LocalHttpServer>();
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

