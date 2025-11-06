using System.Reflection;
using CliWrap;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Jobs;
using R3;
using static NexusMods.Games.RedEngine.Constants;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    private readonly GameToolRunner _toolRunner;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly ILogger _logger;
    private readonly RedModSortOrderVariety _redModSortOrderVariety;

    public RedModDeployTool(GameToolRunner toolRunner, TemporaryFileManager temporaryFileManager, ILogger<RedModDeployTool> logger, RedModSortOrderVariety sortOrderVariety)
    {
        _logger = logger;
        _toolRunner = toolRunner;
        _temporaryFileManager = temporaryFileManager;
        _redModSortOrderVariety = sortOrderVariety;
    }
    
    public IEnumerable<GameId> GameIds => [Cyberpunk2077.Cyberpunk2077Game.GameId];

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

        // Note (halgari): When we change the redmod order, we need to force redmod to re-run. This is due to a bug in redmod
        // not recognizing order as a change in the mod configuration. If that bug ever gets fixed (does an order check) then  
        // we can resolve this by removing the -force flag.
        var fs = FileSystem.Shared;
        if (fs.OS.IsWindows)
        {
            var command = Cli.Wrap(exe.ToString())
                .WithArguments(["deploy", "-force", "-modlist=" + loadorderFile.Path], true)
                .WithWorkingDirectory(exe.Parent.ToString());
            await _toolRunner.ExecuteAsync(loadout, command, true, cancellationToken);
        }
        else
        {
            if (loadout.InstallationInstance.LocatorResultMetadata is SteamLocatorResultMetadata steamLocatorResultMetadata)
            {
                if (steamLocatorResultMetadata.LinuxCompatibilityDataProvider is null) return;
                var wineDirectory = steamLocatorResultMetadata.LinuxCompatibilityDataProvider.WinePrefixDirectoryPath;
                var cDriveDirectory = wineDirectory.Combine("drive_c");

                if (!cDriveDirectory.DirectoryExists()) cDriveDirectory.CreateDirectory();
                var tmpFile = cDriveDirectory.Combine("modlist.txt");
                await using (var input = loadorderFile.Path.Read())
                await using (var output = tmpFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    await input.CopyToAsync(output, cancellationToken: cancellationToken);
                }

                await using var batchPath = await ExtractTemporaryDeployScript();
                var command = Cli.Wrap(batchPath.ToString()).WithArguments(["deploy", "-force", "-modlist=C:\\modlist.txt"], escape: true);
                await _toolRunner.ExecuteAsync(loadout, command, true, cancellationToken);
            }            
            else
            {
                _logger.LogWarning("Skip running redmod, it's only supported for Steam on Linux at the moment");
            }
        }
    }

    internal async Task WriteLoadOrderFile(AbsolutePath loadorderFilePath, Loadout.ReadOnly loadout)
    {
        // TODO: this currently uses the loadout sort order, change this to use the "Active" sort order,
        // once we support switching between collection and loadout sort orders
        var collectionId = Optional.None<CollectionGroupId>();
        
        var output = string.Empty;
        // Note(AL12rs): this will get the Load Order for the specific revision of the DB of loadout, which might not be the latest
        var order = _redModSortOrderVariety.GetRedModOrder(loadout, collectionId, loadout.Db);
        
        // NOTE(erri120): redmod only accepts CRLR line breaks, everything else breaks the program
        // and results in getting errors like `Non-existant mod selected`
        output = order.Count > 0 ? string.Join("\r\n", order) : string.Empty;
        
        await loadorderFilePath.WriteAllTextAsync(output);
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
