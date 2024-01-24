using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.BCL.Extensions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel;

/// <summary>
/// Installs mods from archives
/// </summary>
public class ArchiveInstaller : IArchiveInstaller
{
    private readonly ILogger<ArchiveInstaller> _logger;
    private readonly IDataStore _dataStore;
    private readonly LoadoutRegistry _registry;
    private readonly IActivityFactory _activityFactory;
    private readonly IFileStore _fileStore;
    private readonly IFileOriginRegistry _fileOriginRegistry;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ArchiveInstaller(ILogger<ArchiveInstaller> logger,
        IFileOriginRegistry fileOriginRegistry,
        IDataStore dataStore,
        LoadoutRegistry registry,
        IFileStore fileStore,
        IActivityFactory activityFactory)
    {
        _logger = logger;
        _dataStore = dataStore;
        _fileOriginRegistry = fileOriginRegistry;
        _registry = registry;
        _fileStore = fileStore;
        _activityFactory = activityFactory;
    }

    /// <inheritdoc />
    public async Task<ModId[]> AddMods(LoadoutId loadoutId, DownloadId downloadId, string? defaultModName = null, IModInstaller? installer = null, CancellationToken token = default)
    {
        // Get the loadout and create the mod so we can use it in the job.
        var loadout = _registry.GetMarker(loadoutId);
        var useCustomInstaller = installer != null;

        var download = await _fileOriginRegistry.Get(downloadId);
        var archiveName = "<unknown>";
        if (download.MetaData is not null && defaultModName == null)
        {
            archiveName = download.MetaData.Name;
        }

        var baseMod = new Mod
        {
            Id = ModId.NewId(),
            Name = defaultModName ?? archiveName,
            Files = new EntityDictionary<ModFileId, AModFile>(_dataStore),
            Status = ModStatus.Installing
        };

        var cursor = new ModCursor { LoadoutId = loadoutId, ModId = baseMod.Id };
        loadout.Add(baseMod);

        try
        {
            // Create the job so the UI can show progress.
            using var job = _activityFactory.Create(IArchiveInstaller.Group, "Adding mod files to {Name}", baseMod.Name);

            // Create a tree so installers can find the file easily.
            var tree = TreeCreator.Create(download.Contents, _fileStore);

            // Step 3: Run the archive through the installers.
            var installers = loadout.Value.Installation.Game.Installers;
            if (installer != null)
            {
                installers = new[] { installer };
            }

            var (results, modInstaller) = await installers
                .SelectAsync(async modInstaller =>
                {
                    try
                    {
                        var install = loadout.Value.Installation;
                        var info = new ModInstallerInfo()
                        {
                            ArchiveFiles = tree,
                            BaseModId = baseMod.Id,
                            Locations = install.LocationsRegister,
                            GameName = install.Game.Name,
                            Store = install.Store,
                            Version = install.Version,
                            ModName = baseMod.Name
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


            if (results == null || results.Length == 0)
            {
                if (useCustomInstaller)
                {
                    // User was using an explicit installer, if no files were returned, we can assume the user cancelled the installation.
                    // Remove the mod from the loadout.
                    _registry.Alter(cursor, $"Cancelled installation of {archiveName}", _ => null);
                    return Array.Empty<ModId>();
                }
                _logger.LogError("No Installer found for {Name}", archiveName);
                _registry.Alter(cursor, $"Failed to install mod {archiveName}",m => m! with { Status = ModStatus.Failed });
                throw new NotSupportedException($"No Installer found for {archiveName}");
            }

            var mods = results.Select(result => new Mod
            {
                Id = result.Id,
                Files = result.Files.ToEntityDictionary(_dataStore),
                Name = result.Name ?? baseMod.Name,
                Version = result.Version ?? baseMod.Version,
                SortRules = (result.SortRules ?? Array.Empty<ISortRule<Mod, ModId>>()).ToImmutableList()
            }).WithPersist(_dataStore).ToArray();

            if (mods.Length == 0)
            {
                _registry.Alter(cursor, $"Failed to install mod {archiveName}", m => m! with { Status = ModStatus.Failed});
                throw new NotImplementedException($"The installer returned 0 mods for {archiveName}");
            }

            if (mods.Length == 1)
            {
                mods[0] = mods[0] with
                {
                    Id = baseMod.Id
                };
            }

            job.AddProgress(Percent.CreateClamped(0.75));

            // Step 5: Add the mod to the loadout.
            AModMetadata? modMetadata = mods.Length > 1
                ? new GroupMetadata
                {
                    Id = GroupId.NewId(),
                    CreationReason = GroupCreationReason.MultipleModsOneArchive
                }
                : null;

            var modIds = mods.Select(mod => mod.Id).ToHashSet();
            Debug.Assert(modIds.Count == mods.Length, $"The installer {modInstaller.GetType()} returned mods with non-unique ids.");

            foreach (var mod in mods)
            {
                mod.Metadata = modMetadata;

                if (mod.Id.Equals(baseMod.Id))
                {
                    _registry.Alter(
                        cursor,
                        $"Adding mod files to {baseMod.Name}",
                        x => x! with
                        {
                            Status = ModStatus.Installed,
                            Enabled = true,
                            Name = mod.Name,
                            Version = mod.Version,
                            SortRules = mod.SortRules,
                            Files = mod.Files,
                            Metadata = mod.Metadata
                        });
                }
                else
                {
                    loadout.Add(mod with
                    {
                        Status = ModStatus.Installed,
                        Enabled = true,
                    });
                }
            }

            if (!modIds.Contains(baseMod.Id))
            {
                loadout.Remove(baseMod);
            }

            return mods.Select(x => x.Id).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod {Name}", archiveName);
            _registry.Alter(cursor, $"Failed to install {archiveName}", mod => mod! with
            {
                Status = ModStatus.Failed
            });

            throw;
        }
    }
}
