using System.Collections.Concurrent;
using Bannerlord.LauncherManager;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBladeBannerlord;

public sealed class LauncherManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, LauncherManagerNexusMods> _instances = new();

    public LauncherManagerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    internal LauncherManagerNexusMods Get(GameInstallation installation)
    {
        return _instances.GetOrAdd(installation.Locations[GameFolderType.Game].ToString(),
            static (installationPath, factory) => ValueFactory(factory, installationPath), _loggerFactory);
    }
    
    internal LauncherManagerNexusMods Get(string installationPath)
    {
        return _instances.GetOrAdd(installationPath,
            static (installationPath, factory) => ValueFactory(factory, installationPath), _loggerFactory);
    }

    private static LauncherManagerNexusMods ValueFactory(ILoggerFactory loggerFactory, string installationPath)
    {
        return new LauncherManagerNexusMods(loggerFactory.CreateLogger<LauncherManagerNexusMods>(), installationPath);
    }
}