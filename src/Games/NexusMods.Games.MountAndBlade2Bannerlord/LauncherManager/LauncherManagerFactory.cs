using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Utils;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;

/// <summary>
/// The LauncherManager holds a state per game instance.
///
/// (Aragas)
/// This could be replaced by introducing a game instance DI scope.
/// In that case the LauncherManager could be a scoped service and this factory would not be needed.
/// </summary>
public sealed class LauncherManagerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, LauncherManagerNexusMods> _instances = new();

    public LauncherManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public LauncherManagerNexusMods Get(GameInstallMetadata.ReadOnly installation)
    {
        var store = Converter.ToGameStoreTW(installation.Store);
        return _instances.GetOrAdd(installation.Path,
            static (installationPath, tuple) => ValueFactory(tuple._serviceProvider, installationPath, tuple.store), (_serviceProvider, store));
    }

    public LauncherManagerNexusMods Get(GameInstallation installation)
    {
        var store = Converter.ToGameStoreTW(installation.Store);
        return _instances.GetOrAdd(installation.LocationsRegister[LocationId.Game].ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._serviceProvider, installationPath, tuple.store), (_serviceProvider, store));
    }

    public LauncherManagerNexusMods Get(GameLocatorResult gameLocator)
    {
        var store = Converter.ToGameStoreTW(gameLocator.Store);
        return _instances.GetOrAdd(gameLocator.Path.ToString(),
            static (installationPath, tuple) => ValueFactory(tuple._serviceProvider, installationPath, tuple.store), (_serviceProvider, store));
    }

    public LauncherManagerNexusMods Get(string installationPath, Bannerlord.LauncherManager.Models.GameStore store)
    {
        return _instances.GetOrAdd(installationPath,
            static (installationPath, tuple) => ValueFactory(tuple._serviceProvider, installationPath, tuple.store), (_serviceProvider, store));
    }

    private static LauncherManagerNexusMods ValueFactory(IServiceProvider serviceProvider, string installationPath, Bannerlord.LauncherManager.Models.GameStore store)
    {
        return new LauncherManagerNexusMods(serviceProvider, installationPath, store);
    }
}
