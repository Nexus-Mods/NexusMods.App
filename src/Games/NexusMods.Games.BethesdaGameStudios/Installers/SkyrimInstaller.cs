using System.Diagnostics;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Installers;

/// <summary>
/// Installs Skyrim files; both plugins and loose files.
/// </summary>
/// <remarks>
///    This installer allows mods which use the following formats (where {Root} is archive root):
///    - {Root}/{AnyFolderName}
///    - {Root}/Data
///    - {Root}
///
///    Inside these folders, you can either have .esp, .esm, .esl or 'meshes' and 'textures' folders.
///    {AnyFolderName} allows only 1 level of nesting.
/// </remarks>
public class SkyrimInstaller : IModInstaller
{
    private static readonly RelativePath DataFolder = "Data";
    private static readonly RelativePath MeshesFolder = "meshes";
    private static readonly RelativePath TexturesFolder = "textures";

    private static readonly Extension MeshExtension = new(".nif");
    private static readonly Extension TextureExtension = new(".dds");
    private static readonly Extension EspExtension = new(".esp");
    private static readonly Extension EslExtension = new(".esl");
    private static readonly Extension EsmExtension = new(".esm");
    private static readonly Extension BsaExtension = new(".bsa");

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (installation.Game is not SkyrimSpecialEdition)
            return Priority.None;

        // Determine one of the following:
        // - There is a 'meshes' folder with NIF file.
        // - There is a 'textures' folder with DDS file.
        // - There is a folder (max 1 level deep) with either of the following cases.
        foreach (var kv in archiveFiles)
        {
            var (path, _) = kv;

            // Check in subfolder first
            if (path.Depth != 0)
            {
                var child = path.DropFirst(numDirectories: 1);
                if (AssertPathForPriority(child)) return Priority.Normal;
            }

            if (AssertPathForPriority(path)) return Priority.Normal;
        }

        return Priority.None;
    }

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(baseModId, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<ModInstallerResult> GetMods(
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var prefix = FindFolderPrefixForExtract(archiveFiles);
        var modFiles = archiveFiles
            .Where(kv => prefix == RelativePath.Empty || kv.Key.InFolder(prefix))
            .Select(kv =>
            {
                var (path, file) = kv;
                var relative = path.RelativeTo(prefix);

                return file.ToFromArchive(
                    new GamePath(GameFolderType.Game, DataFolder.Join(relative))
                );
            });

        yield return new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        };
    }

    private static RelativePath FindFolderPrefixForExtract(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        // Note: We already determined in priority testing that this is something
        // we want to extract, so just find the folder with meshes/textures subfolders
        // and roll with that.

        // Normally we could cache this stuff between priority and extract but there's
        // no guarantee functions will be ran in that order and another file wouldn't be
        // checked somewhere in the middle for example.
        foreach (var kv in files)
        {
            var (path, _) = kv;

            // foo/bar/baz
            // 1) child: foo/bar/baz -> bar/baz
            // 2) top-parent: foo/bar/baz -> foo

            // Check in subfolder first
            if (path.Depth != 0)
            {
                var child = path.DropFirst(numDirectories: 1);
                if (AssertFolderForExtract(child)) return path.TopParent;
            }

            if (AssertFolderForExtract(path)) return RelativePath.Empty;
        }

        throw new UnreachableException();
    }

    private static bool AssertPathForPriority(RelativePath relativePath)
    {
        if (relativePath.InFolder(MeshesFolder) && relativePath.Extension == MeshExtension)
            return true;

        if (relativePath.InFolder(TexturesFolder) && relativePath.Extension == TextureExtension)
            return true;

        // Has plugins in current directory.
        if (relativePath.Depth != 0) return false;

        if (relativePath.Extension == EsmExtension) return true;
        if (relativePath.Extension == EspExtension) return true;
        if (relativePath.Extension == EslExtension) return true;
        if (relativePath.Extension == BsaExtension) return true;

        return false;
    }

    private static bool AssertFolderForExtract(RelativePath relativePath)
    {
        if (relativePath.InFolder(MeshesFolder)) return true;
        if (relativePath.InFolder(TexturesFolder)) return true;

        // Check for plugins, subdirectory and extension check here should be sufficient.
        if (relativePath.Depth != 0) return false;

        if (relativePath.Extension == EsmExtension) return true;
        if (relativePath.Extension == EspExtension) return true;
        if (relativePath.Extension == EslExtension) return true;
        if (relativePath.Extension == BsaExtension) return true;

        return false;
    }
}
