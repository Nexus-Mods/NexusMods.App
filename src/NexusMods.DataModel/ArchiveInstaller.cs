using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel;

/// <summary>
/// Installs mods from archives previously analyzed by <see cref="IArchiveAnalyzer"/>.
/// </summary>
public class ArchiveInstaller : IArchiveInstaller
{
    private readonly ILogger<ArchiveInstaller> _logger;
    private readonly IDataStore _dataStore;
    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly LoadoutRegistry _registry;
    private readonly IModInstaller[] _modInstallers;
    private readonly IInterprocessJobManager _jobManager;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ArchiveInstaller(ILogger<ArchiveInstaller> logger,
        IArchiveAnalyzer archiveAnalyzer,
        IDataStore dataStore,
        LoadoutRegistry registry,
        IEnumerable<IModInstaller> modInstallers,
        IInterprocessJobManager jobManager)
    {
        _logger = logger;
        _dataStore = dataStore;
        _registry = registry;
        _archiveAnalyzer = archiveAnalyzer;
        _jobManager = jobManager;
        _modInstallers = modInstallers.ToArray();
    }

    /// <inheritdoc />
    public async Task<ModId[]> AddMods(LoadoutId loadoutId, Hash archiveHash, string? defaultModName = null, CancellationToken token = default)
    {
        if (_archiveAnalyzer.GetAnalysisData(archiveHash) is not AnalyzedArchive analysisData)
        {
            _logger.LogError("Could not find analysis data for archive {ArchiveHash} or file is not an archive", archiveHash);
            throw new InvalidOperationException("Could not find analysis data for archive");
        }

        // Get the loadout and create the mod so we can use it in the job.
        var loadout = _registry.GetMarker(loadoutId);

        var metaData = AArchiveMetaData.GetMetaDatas(_dataStore, archiveHash).FirstOrDefault();
        var archiveName = "<unknown>";
        if (metaData is not null && defaultModName == null)
        {
            archiveName = metaData.Name;
        }

        var baseMod = new Mod
        {
            Id = ModId.New(),
            Name = archiveName,
            Files = new EntityDictionary<ModFileId, AModFile>(_dataStore),
            Status = ModStatus.Installing
        };

        var cursor = new ModCursor { LoadoutId = loadoutId, ModId = baseMod.Id };
        loadout.Add(baseMod);

        try
        {
            // Create the job so the UI can show progress.
            using var job = InterprocessJob.Create(_jobManager, new AddModJob
            {
                ModId = baseMod.Id,
                LoadoutId = loadoutId
            });

            // Step 3: Run the archive through the installers.
            var installer = _modInstallers
                .Select(i => (Installer: i, Priority: i.GetPriority(loadout.Value.Installation, analysisData.Contents)))
                .Where(p => p.Priority != Priority.None)
                .OrderBy(p => p.Priority)
                .FirstOrDefault();

            if (installer == default)
            {
                _logger.LogError("No Installer found for {Name}", archiveName);
                _registry.Alter(cursor, $"Failed to install mod {archiveName}",m => m! with { Status = ModStatus.Failed });
                throw new NotSupportedException($"No Installer found for {archiveName}");
            }

            // Step 4: Install the mods.
            var results = await installer.Installer.GetModsAsync(
                loadout.Value.Installation,
                baseMod.Id,
                analysisData.Hash,
                analysisData.Contents,
                token
            );

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

            job.Progress = new Percent(0.75);

            // Step 5: Add the mod to the loadout.
            AModMetadata? modMetadata = mods.Length > 1
                ? new GroupMetadata
                {
                    Id = GroupId.New(),
                    CreationReason = GroupCreationReason.MultipleModsOneArchive
                }
                : null;

            var modIds = mods.Select(mod => mod.Id).ToHashSet();
            Debug.Assert(modIds.Count == mods.Length, $"The installer {installer.Installer.GetType()} returned mods with non-unique ids.");

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
