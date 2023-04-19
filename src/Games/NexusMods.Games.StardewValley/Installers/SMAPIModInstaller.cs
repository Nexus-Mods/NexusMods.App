using System.Diagnostics;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.StardewValley.Analyzers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for mods that use the Stardew Modding API (SMAPI).
/// </summary>
public class SMAPIModInstaller : IModInstaller
{
    private static readonly RelativePath ModsFolder = "Mods".ToRelativePath();
    private static readonly RelativePath ManifestFile = "manifest.json".ToRelativePath();

    private readonly IDataStore _dataStore;

    public SMAPIModInstaller(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    private static bool TryGetManifestFile(
        EntityDictionary<RelativePath, AnalyzedFile> files,
        out KeyValuePair<RelativePath, AnalyzedFile> manifestFile)
    {
        return files.TryGetFirst(kv =>
                kv.Key.FileName.Equals(ManifestFile) &&
                kv.Value.AnalysisData.Any(x => x is SMAPIManifest),
            out manifestFile);
    }

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<StardewValley>()) return Common.Priority.None;
        return TryGetManifestFile(files, out _)
            ? Common.Priority.Highest
            : Common.Priority.None;
    }

    public ValueTask<IEnumerable<Mod>> GetModsAsync(
        GameInstallation gameInstallation,
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(baseMod, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<Mod> GetMods(
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        // TODO: update this installer to work with multiple manifests and return multiple mods

        if (!TryGetManifestFile(archiveFiles, out var manifestFile))
            throw new UnreachableException($"{nameof(SMAPIModInstaller)} should guarantee with {nameof(Priority)} that {nameof(GetModsAsync)} is never called for archives that don't have a manifest file.");

        var parent = manifestFile.Key.Parent;

        var modFiles = archiveFiles
            .Where(kv => kv.Key.InFolder(parent))
            .Select(kv =>
            {
                var (path, file) = kv;

                return new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchiveHash, path),
                    To = new GamePath(GameFolderType.Game, ModsFolder.Join(path.DropFirst(parent.Depth))),
                    Hash = file.Hash,
                    Size = file.Size
                };
            });

        yield return baseMod with
        {
            Files = modFiles.ToEntityDictionary(_dataStore)
        };
    }
}
