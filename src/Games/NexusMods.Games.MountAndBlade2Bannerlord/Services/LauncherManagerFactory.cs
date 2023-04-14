using System.Collections.Concurrent;
using Bannerlord.LauncherManager.Models;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Services;

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
        // TODO:
        var store = GameStore.Steam;
        return _instances.GetOrAdd(installation.Locations[GameFolderType.Game].ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }
    internal LauncherManagerNexusMods Get(GameLocatorResult gameLocator)
    {
        // TODO:
        var store = GameStore.Steam;
        return _instances.GetOrAdd(gameLocator.Path.ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    internal LauncherManagerNexusMods Get(string installationPath, GameStore store)
    {
        return _instances.GetOrAdd(installationPath,
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    private static LauncherManagerNexusMods ValueFactory(ILoggerFactory loggerFactory, string installationPath, GameStore store)
    {
        return new LauncherManagerNexusMods(loggerFactory.CreateLogger<LauncherManagerNexusMods>(), installationPath, store);
    }
}
