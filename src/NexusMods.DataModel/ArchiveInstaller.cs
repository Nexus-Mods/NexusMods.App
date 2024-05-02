using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.DataModel;

/// <summary>
/// Installs mods from archives
/// </summary>
public class ArchiveInstaller : IArchiveInstaller
{
    private readonly ILogger<ArchiveInstaller> _logger;
    private readonly IConnection _conn;
    private readonly IActivityFactory _activityFactory;
    private readonly IFileStore _fileStore;
    private readonly IFileOriginRegistry _fileOriginRegistry;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ArchiveInstaller(ILogger<ArchiveInstaller> logger,
        IFileOriginRegistry fileOriginRegistry,
        IConnection conn,
        IFileStore fileStore,
        IActivityFactory activityFactory)
    {
        _logger = logger;
        _conn = conn;
        _fileOriginRegistry = fileOriginRegistry;
        _fileStore = fileStore;
        _activityFactory = activityFactory;
    }
    
    /// <inheritdoc />
    public async Task<ModId[]> AddMods(LoadoutId loadoutId, DownloadAnalysis.Model download, string? defaultModName = null, IModInstaller? installer = null, CancellationToken token = default)
    {
        // Get the loadout and create the mod, so we can use it in the job.
        var useCustomInstaller = installer != null;
        var loadout = _conn.Db.Get<Loadout.Model>(loadoutId.Value);
        
        var archiveName = "<unknown>";
        if (download.Contains(DownloadAnalysis.SuggestedName))
        {
            archiveName = download.Get(DownloadAnalysis.SuggestedName);
        }
        
        string modName = defaultModName ?? archiveName;

        ModId modId;
        Mod.Model baseMod;
        {
            using var tx = _conn.BeginTransaction();

            baseMod = new Mod.Model(tx)
            {
                Name = modName,
                Source = download,
                Status = ModStatus.Installing,
                Loadout = loadout,
            };
            loadout.Revise(tx);
            var result = await tx.Commit();
            baseMod = result.Remap(baseMod);
            modId = ModId.From(result[baseMod.Id]);
        }

        try
        {
            // Create the job so the UI can show progress.
            using var job = _activityFactory.Create(IArchiveInstaller.Group, "Adding mod files to {Name}", modName);

            // Create a tree so installers can find the file easily.
            var tree = download.GetFileTree(_fileStore);

            // Step 3: Run the archive through the installers.
            var installers = loadout.Installation.GetGame().Installers;
            if (installer != null)
            {
                installers = new[] { installer };
            }

            var (results, modInstaller) = await installers
                .SelectAsync(async modInstaller =>
                {
                    try
                    {
                        var install = loadout.Installation;
                        var info = new ModInstallerInfo
                        {
                            ArchiveFiles = tree,
                            BaseModId = modId,
                            Locations = install.LocationsRegister,
                            GameName = install.Game.Name,
                            Store = install.Store,
                            Version = install.Version,
                            ModName = modName,
                            Source = download,
                        };

                        var modResults = (await modInstaller.GetModsAsync(info, token)).ToArray();
                        return (modResults, modInstaller);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get mods from installer {Installer}", modInstaller.GetType());
                        return (Array.Empty<ModInstallerResult>(), modInstaller);
                    }
                })
                .FirstOrDefault(result => result.Item1.Any());

            if (results is null || results.Length == 0)
            {
                if (useCustomInstaller)
                {
                    // User was using an explicit installer, if no files were returned, we can assume the user cancelled the installation.
                    // Remove the mod from the loadout.
                    await SetStatus(modId, ModStatus.Failed);
                    return [];
                }

                _logger.LogError("No Installer found for {Name}", archiveName);
                await SetStatus(modId, ModStatus.Failed);
                throw new NotSupportedException($"No Installer found for {archiveName}");
            }

            if (results.Length == 0)
            {
                await SetStatus(modId, ModStatus.Failed);
                throw new NotImplementedException($"The installer returned 0 mods for {archiveName}");
            }
            
            // Step 4: Add the mods to the loadout
            using var tx = _conn.BeginTransaction();
            var modIds = new List<EntityId>();

            for (var idx = 0; idx < results.Length; idx++)
            {
                var entity = new TempEntity();
                var result = results[idx];
                if (idx == 0)
                {
                    entity.Id = modId.Value;
                    baseMod.Revise(tx);
                }

                entity.Add(Mod.Name, result.Name ?? modName);
                entity.Add(Mod.Loadout, loadoutId.Value);
                entity.Add(Mod.Status, ModStatus.Installed);
                entity.Add(Mod.Version, result.Version ?? "<unknown>");
                entity.Add(Mod.Enabled, true);
                entity.Add(Mod.Source, download.Id);
                entity.Add(Mod.Category, ModCategory.Mod);
                
                entity.AddTo(tx);
                modIds.Add(entity.Id!.Value);
                
                
                foreach (var file in result.Files)
                {
                    file.Add(File.Loadout, loadoutId.Value);
                    file.Add(File.Mod, entity.Id!.Value);
                    file.AddTo(tx);
                }
            }
            
            var finalResult = await tx.Commit();
            _logger.LogInformation("Added {Count} mods to {Loadout}", results.Length, loadout.Name);
            return modIds.Select(id => ModId.From(finalResult[id])).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod {Name}", archiveName);
            await SetStatus(modId, ModStatus.Failed);
            throw;
        }
    }

    private async Task SetStatus(ModId modId, ModStatus status)
    {
        var mod = _conn.Db.Get<Mod.Model>(modId.Value);
        _logger.LogInformation("Setting status of ModId:{ModId}({Name}) to {Status}", modId, mod.Name, status);
        
        using var tx = _conn.BeginTransaction();
        tx.Add(modId.Value, Mod.Status, status);
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task<ModId[]> AddMods(LoadoutId loadoutId, DownloadId downloadId, string? defaultModName = null, 
        IModInstaller? installer = null, CancellationToken token = default)
    {
        var download = _fileOriginRegistry.Get(downloadId);
        
        return await AddMods(loadoutId, download, defaultModName, installer, token);
    }

}
