using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Services;

public sealed class LauncherManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, LauncherManagerNexusMods> _instances = new();

    public LauncherManagerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public LauncherManagerNexusMods Get(ModInstallerInfo info)
    {
        var store = Converter.ToGameStoreTW(info.Store);
        return _instances.GetOrAdd(info.Locations[LocationId.Game].ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    public LauncherManagerNexusMods Get(GameInstallation installation)
    {
        var store = Converter.ToGameStoreTW(installation.Store);
        return _instances.GetOrAdd(installation.LocationsRegister[LocationId.Game].ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    public LauncherManagerNexusMods Get(GameLocatorResult gameLocator)
    {
        var store = Converter.ToGameStoreTW(gameLocator.Store);
        return _instances.GetOrAdd(gameLocator.Path.ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    public LauncherManagerNexusMods Get(string installationPath, Bannerlord.LauncherManager.Models.GameStore store)
    {
        return _instances.GetOrAdd(installationPath,
            static (installationPath, tuple) => ValueFactory(tuple._loggerFactory, installationPath, tuple.store), (_loggerFactory, store));
    }

    private static LauncherManagerNexusMods ValueFactory(ILoggerFactory loggerFactory, string installationPath, Bannerlord.LauncherManager.Models.GameStore store)
    {
        return new LauncherManagerNexusMods(loggerFactory.CreateLogger<LauncherManagerNexusMods>(), installationPath, store);
    }
}
