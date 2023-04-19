using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;
using NexusMods.Paths;

using GameStore = NexusMods.DataModel.Games.GameStore;
using GameStoreTW = Bannerlord.LauncherManager.Models.GameStore;

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
        var store = Converter.ToGameStoreTW(installation.Store);
        return _instances.GetOrAdd(installation.Locations[GameFolderType.Game].ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }
    internal LauncherManagerNexusMods Get(GameLocatorResult gameLocator)
    {
        var store = Converter.ToGameStoreTW(gameLocator.Store);
        return _instances.GetOrAdd(gameLocator.Path.ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    internal LauncherManagerNexusMods Get(string installationPath, GameStoreTW store)
    {
        return _instances.GetOrAdd(installationPath,
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    private static LauncherManagerNexusMods ValueFactory(ILoggerFactory loggerFactory, string installationPath, GameStoreTW store)
    {
        return new LauncherManagerNexusMods(loggerFactory.CreateLogger<LauncherManagerNexusMods>(), installationPath, store);
    }
}
