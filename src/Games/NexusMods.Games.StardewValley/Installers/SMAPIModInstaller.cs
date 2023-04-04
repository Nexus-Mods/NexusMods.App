using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
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
    private static RelativePath ModsFolder = "Mods".ToRelativePath();

    private static KeyValuePair<RelativePath, AnalyzedFile>? GetManifestFile(
        EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return files.FirstOrDefault(kv =>
            kv.Key.FileName.Equals("manifest.json") &&
            kv.Value.AnalysisData.Any(x => x is SMAPIManifest));
    }

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<StardewValley>()) return Common.Priority.None;

        var manifestFile = GetManifestFile(files);
        return manifestFile is null ? Common.Priority.None : Common.Priority.Highest;
    }

    public ValueTask<IEnumerable<AModFile>> GetFilesToExtractAsync(GameInstallation installation,
        Hash srcArchiveHash, EntityDictionary<RelativePath, AnalyzedFile> files,
        CancellationToken ct = default)
    {
        return ValueTask.FromResult(GetFilesToExtract(srcArchiveHash, files));
    }

    private static IEnumerable<AModFile> GetFilesToExtract(
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return files.Select(kv =>
        {
            var (path, file) = kv;
            return new FromArchive
            {
                Size = file.Size,
                Hash = file.Hash,
                Id = ModFileId.New(),
                From = new HashRelativePath(srcArchiveHash, path),
                To = new GamePath(GameFolderType.Game, ModsFolder.Join(path)),
            };
        });

    }
}
