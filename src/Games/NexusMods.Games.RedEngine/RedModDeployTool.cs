using System.Reflection;
using CliWrap;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;
using NexusMods.Paths;
using R3;
using static NexusMods.Games.RedEngine.Constants;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    private readonly GameToolRunner _toolRunner;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly ILogger _logger;
    private readonly RedModSortableItemProviderFactory _sortableItemProviderFactory;

    public RedModDeployTool(GameToolRunner toolRunner, TemporaryFileManager temporaryFileManager, ILogger<RedModDeployTool> logger, RedModSortableItemProviderFactory sortableItemProviderFactory)
    {
        _logger = logger;
        _toolRunner = toolRunner;
        _temporaryFileManager = temporaryFileManager;
        _sortableItemProviderFactory = sortableItemProviderFactory;
    }
    
    public IEnumerable<GameId> GameIds => [Cyberpunk2077.Cyberpunk2077Game.GameIdStatic];

    public async Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var exe = RedModPath.CombineChecked(loadout.InstallationInstance);
        var deployFolder = RedModDeployFolder.CombineChecked(loadout.InstallationInstance);

        await using var loadorderFile = _temporaryFileManager.CreateFile();
        await WriteLoadOrderFile(loadorderFile.Path, loadout);
        
        // RedMod deploys to this folder and freaks out if it doesn't exist, but our synchronizer will
        // delete it if it's empty, so we need to recreate it here.
        if (!deployFolder.DirectoryExists())
            deployFolder.CreateDirectory();

        var fs = FileSystem.Shared;
        if (fs.OS.IsWindows)
        {
            var command = Cli.Wrap(exe.ToString())
                .WithArguments(["deploy", "--modlist=" + loadorderFile.Path], true)
                .WithWorkingDirectory(exe.Parent.ToString());
            await _toolRunner.ExecuteAsync(loadout, command, true, cancellationToken);
        }
        else
        {
            if (loadout.InstallationInstance.LocatorResultMetadata is SteamLocatorResultMetadata)
            {
                await using var batchPath = await ExtractTemporaryDeployScript();
                await _toolRunner.ExecuteAsync(loadout, Cli.Wrap(batchPath.ToString()), true, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Skip running redmod, it's only supported for Steam on Linux at the moment");
            }
        }
    }

    internal async Task WriteLoadOrderFile(AbsolutePath loadorderFilePath, Loadout.ReadOnly loadout)
    {
        var provider = (RedModSortableItemProvider)_sortableItemProviderFactory.GetLoadoutSortableItemProvider(loadout);
        // Note: this will get the Load Order for the specific revision of the DB of loadout, which might not be the latest
        var order = provider.GetRedModOrder(loadout.Db);

        await loadorderFilePath.WriteAllLinesAsync(order);
    }

    public string Name => "RedMod Deploy";

    public IJobTask<ITool, Unit> StartJob(Loadout.ReadOnly loadout, IJobMonitor monitor, CancellationToken cancellationToken)
    {
        return monitor.Begin<ITool, Unit>(this, async _ =>
        {
            await Execute(loadout, cancellationToken);
            return Unit.Default;
        });
    }

    private async Task<TemporaryPath> ExtractTemporaryDeployScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "deploy_redmod.bat";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Resource {resourceName} not found in assembly.");

        using var reader = new StreamReader(stream);
        var file = _temporaryFileManager.CreateFile((Extension?)".bat");
        await file.Path.WriteAllTextAsync(await reader.ReadToEndAsync());
        return file;
    }
}
