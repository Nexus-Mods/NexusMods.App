using Bannerlord.LauncherManager.Utils;
using Bannerlord.ModuleManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager;
namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// This is to run the game or SMAPI using the shell, which allows them to start their own console,
/// allowing users to interact with it.
/// </summary>
public class BannerlordRunGameTool : RunGameTool<Bannerlord>
{
    private readonly ILogger<BannerlordRunGameTool> _logger;
    private IServiceProvider _serviceProvider;
    
    public BannerlordRunGameTool(IServiceProvider serviceProvider, Bannerlord game)
        : base(serviceProvider, game)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<BannerlordRunGameTool>>();
    }

    protected override bool UseShell { get; set; } = false;
    
    public override async Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken, string[]? commandLineArgs)
    {
        commandLineArgs ??= [];

        // We need to 'inject' the current set of enabled modules in addition to any existing parameters.
        // This way, external arguments specified by outside entities are preserved.

        // Set the (automatic) load order.
        // Copied from Bannerlord.LauncherManager
        // will make PR upstream after this is merged to expose this logic in a way
        // that does not require external dependencies of things we don't need or
        // want to provide.
        var manifestPipeline = Pipelines.GetManifestPipeline(_serviceProvider);
        var modules = (await Helpers.GetAllManifestsAsync(_logger, loadout, manifestPipeline, cancellationToken).ToArrayAsync(cancellationToken))
            .Select(x => x.Item2)
            .Concat(Hack.GetDummyBaseGameModules());
        var sortedModules = AutoSort(modules).Select(x => x.Id).ToArray();
        var loadOrderCli = sortedModules.Length > 0 ? $"_MODULES_*{string.Join("*", sortedModules)}*_MODULES_" : string.Empty;

        // Add the new arguments
        commandLineArgs = commandLineArgs.Concat(["/singleplayer", loadOrderCli]).ToArray();
        await base.Execute(loadout, cancellationToken, commandLineArgs);
    }
    
    // Copied from Bannerlord.LauncherManager
    // needs downstream changes, will do those changes when possible
    private static IEnumerable<ModuleInfoExtended> AutoSort(IEnumerable<ModuleInfoExtended> source)
    {
        var orderedModules = source
            .OrderByDescending(x => x.IsOfficial)
            .ThenBy(x => x.Id, new AlphanumComparatorFast())
            .ToArray();

        return ModuleSorter.TopologySort(orderedModules, module => ModuleUtilities.GetDependencies(orderedModules, module));
    }
}
