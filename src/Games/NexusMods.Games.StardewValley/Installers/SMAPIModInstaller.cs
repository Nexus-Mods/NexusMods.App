using System.Diagnostics;
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
    private static readonly RelativePath ModsFolder = "Mods".ToRelativePath();
    private static readonly RelativePath ManifestFile = "manifest.json".ToRelativePath();

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
        if (!TryGetManifestFile(files, out var manifestFile))
            throw new UnreachableException($"{nameof(SMAPIModInstaller)} should guarantee with {nameof(Priority)} that {nameof(GetFilesToExtractAsync)} is never called for archives that don't have a manifest file.");

        var parent = manifestFile.Key.Parent;

        foreach (var kv in files)
        {
            var (path, file) = kv;
            if (!path.InFolder(parent)) continue;
            var to = new GamePath(
                GameFolderType.Game,
                ModsFolder.Join(path.DropFirst(parent.Depth))
            );

            yield return new FromArchive
            {
                Size = file.Size,
                Hash = file.Hash,
                Id = ModFileId.New(),
                From = new HashRelativePath(srcArchiveHash, path),
                To = to
            };
        }
    }
}
