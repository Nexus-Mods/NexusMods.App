using GameFinder.Wine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.Backend.Games.Locators;

internal class WinePrefixWrappingLocator : IGameLocator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ILoggerFactory _loggerFactory;

    private readonly LocatorFactory[] _locatorFactories;
    private readonly IWinePrefixManager<WinePrefix> _winePrefixManager;

    public delegate IGameLocator LocatorFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IFileSystem fileSystem, GameFinder.RegistryUtils.IRegistry registry);

    public WinePrefixWrappingLocator(IServiceProvider serviceProvider, LocatorFactory[] locatorFactories)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<WinePrefixWrappingLocator>>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _locatorFactories = locatorFactories;

        _winePrefixManager = new DefaultWinePrefixManager(
            fileSystem: serviceProvider.GetRequiredService<IFileSystem>()
        );
    }

    public IEnumerable<GameLocatorResult> Locate()
    {
        var allPrefix = new List<AWinePrefix>();

        foreach (var result in _winePrefixManager.FindPrefixes())
        {
            if (result.TryPickT1(out var errorMessage, out var winePrefix))
            {
                _logger.LogWarning("Error locating WINE prefix: {ErrorMessage}", errorMessage.Message);
                continue;
            }

            allPrefix.Add(winePrefix);
        }

        var results = new List<GameLocatorResult>();

        foreach (var winePrefix in allPrefix)
        {
            var overlayFileSystem = winePrefix.CreateOverlayFileSystem(_fileSystem);
            var registry = winePrefix.CreateRegistry(overlayFileSystem);

            foreach (var factory in _locatorFactories)
            {
                try
                {
                    var locator = factory(_serviceProvider, _loggerFactory, overlayFileSystem, registry);
                    results.AddRange(locator.Locate());
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception in locator created from factory");
                }
            }
        }

        return results;
    }
}
