using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.BuildInfo;
using NexusMods.Extensions.BCL;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using File = NexusMods.Abstractions.Loadouts.Files.File;
using LibraryFile = NexusMods.Abstractions.Library.Models.LibraryFile;

namespace NexusMods.DataModel;

/// <summary>
/// Installs mods from archives
/// </summary>
public class ArchiveInstaller : IArchiveInstaller
{
    private readonly ILogger<ArchiveInstaller> _logger;
    private readonly ILibraryService _libraryService;
    private readonly IConnection _conn;
    private readonly IActivityFactory _activityFactory;
    private readonly IFileStore _fileStore;
    private readonly IFileOriginRegistry _fileOriginRegistry;
    private readonly IServiceProvider _provider;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ArchiveInstaller(
        ILogger<ArchiveInstaller> logger,
        ILibraryService libraryService,
        IFileOriginRegistry fileOriginRegistry,
        IConnection conn,
        IFileStore fileStore,
        IActivityFactory activityFactory,
        IServiceProvider provider)
    {
        _logger = logger;
        _libraryService = libraryService;
        _conn = conn;
        _fileOriginRegistry = fileOriginRegistry;
        _fileStore = fileStore;
        _activityFactory = activityFactory;
        _provider = provider;
    }

    private async Task ShadowTrafficTestLibraryService(Hash hash, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        // TODO: https://github.com/Nexus-Mods/NexusMods.App/issues/1763
        if (!CompileConstants.IsDebug) return;
        try
        {
            if (!LibraryFile.FindByHash(_conn.Db, hash).TryGetFirst(out var libraryFile))
            {
                _logger.LogDebug("Found no library item with hash `{Hash}`, skipping shadow traffic test", hash);
                return;
            }

            await using var job = _libraryService.InstallItem(libraryFile.AsLibraryItem(), loadout);
            await job.StartAsync(cancellationToken: cancellationToken);
            var result = await job.WaitToFinishAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("InstallItem result: `{Result}`", result.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception install library item");
        }
    }

    /// <inheritdoc />
    public async Task<ModId[]> AddMods(
        LoadoutId loadoutId, 
        DownloadAnalysis.ReadOnly download, 
        string? name = null, 
        IModInstaller? installer = null, 
        CancellationToken token = default)
    {
        // Get the loadout and create the mod, so we can use it in the job.
        var useCustomInstaller = installer != null;
        var loadout = Loadout.Load(_conn.Db, loadoutId);

        await ShadowTrafficTestLibraryService(download.Hash, loadout, token);

        // Note(suggestedName) cannot be null here.
        // Because string is non-nullable where it is set (FileOriginRegistry),
        // and using that is a prerequisite to calling this function.
        var modName = name ?? download.SuggestedName;
        
        ModId modId;
        Mod.ReadOnly baseMod;
        {
            using var tx = _conn.BeginTransaction();

            var newMod = new Mod.New(tx)
            {
                Name = modName,
                SourceId = download,
                Status = ModStatus.Installing,
                LoadoutId = loadout,
                Category = ModCategory.Mod,
                Enabled = true,
                Revision = 0,
            };
            loadout.Revise(tx);
            var result = await tx.Commit();
            baseMod = result.Remap(newMod);
            modId = ModId.From(result[baseMod.Id]);
        }

        try
        {
            // Create the job so the UI can show progress.
            using var job = _activityFactory.Create(IArchiveInstaller.Group, "Adding mod files to {Name}", modName);

            // Create a tree so installers can find the file easily.
            var tree = download.GetFileTree(_fileStore);

            // Step 3: Run the archive through the installers.
            var installers = loadout.InstallationInstance.GetGame().Installers;
            try
            {
                var advancedInstaller = _provider.GetRequiredKeyedService<IModInstaller>("AdvancedManualInstaller");
                installers = installers.Append(advancedInstaller);

            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("AdvancedManualInstaller not found, fallback will not be available");
            }
            
            if (installer != null)
            {
                installers = new[] { installer };
            }

            var (results, modInstaller) = await installers
                .SelectAsync(async modInstaller =>
                {
                    try
                    {
                        var install = loadout.InstallationInstance;
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
                    await SetFailedStatus(modId);
                    return [];
                }

                _logger.LogError("No Installer found for {Name}", modName);
                throw new NotSupportedException($"No Installer found for {modName}");
            }

            if (results.Length == 0)
            {
                throw new NotSupportedException($"The installer returned 0 mods for {modName}");
            }
            
            // Step 4: Add the mods to the loadout
            using var tx = _conn.BeginTransaction();
            var modIds = new List<EntityId>();

            for (var idx = 0; idx < results.Length; idx++)
            {
                var result = results[idx];
                var entity = result.Metadata ?? [];
                
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
            _logger.LogError(ex, "Failed to install mod {Name}", modName);
            await SetFailedStatus(modId);
            throw;
        }
    }

    private async Task SetFailedStatus(ModId modId)
    {
        var mod = Mod.Load(_conn.Db, modId);
        _logger.LogInformation("Setting status of ModId:{ModId}({Name}) to {Status}", modId, mod.Name, ModStatus.Failed);

        using var tx = _conn.BeginTransaction();
        tx.Add(modId.Value, Mod.Name, $"Installation failed: {mod.Name}");
        tx.Add(modId.Value, Mod.Status, ModStatus.Failed);
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task<ModId[]> AddMods(
        LoadoutId loadoutId, 
        DownloadId downloadId, 
        string? name = null,
        IModInstaller? installer = null, 
        CancellationToken token = default)
    {
        var download = _fileOriginRegistry.Get(downloadId);
        
        return await AddMods(loadoutId, download, name, installer, token);
    }

}
