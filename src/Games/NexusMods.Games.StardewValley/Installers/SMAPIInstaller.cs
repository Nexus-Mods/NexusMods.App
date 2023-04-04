using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for SMAPI itself. This is different from <see cref="SMAPIModInstaller"/>,
/// which is an implementation of <see cref="IModInstaller"/> for mods that use SMAPI.
/// </summary>
public class SMAPIInstaller : IModInstaller
{
    private IDataStore _dataStore;

    public SMAPIInstaller(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    private static KeyValuePair<RelativePath, AnalyzedFile>[] GetInstallDataFiles(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var installDataFiles = files.Where(kv =>
        {
            var (path, file) = kv;
            var fileName = path.FileName;
            var parent = path.Parent.FileName;

            return file.FileTypes.Contains(FileType.ZIP) &&
                   fileName.Equals("install.dat") &&
                   (parent.Equals("linux") ||
                    parent.Equals("macOS") ||
                    parent.Equals("windows"));
        }).ToArray();

        return installDataFiles;
    }

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<StardewValley>()) return Common.Priority.None;

        var installDataFiles = GetInstallDataFiles(files);
        return installDataFiles.Length == 3
            ? Common.Priority.Highest
            : Common.Priority.None;
    }

    public ValueTask<IEnumerable<AModFile>> GetFilesToExtractAsync(GameInstallation installation,
        Hash srcArchiveHash, EntityDictionary<RelativePath, AnalyzedFile> files,
        CancellationToken ct = default)
    {
        return ValueTask.FromResult(GetFilesToExtract(installation, files));
    }

    private IEnumerable<AModFile> GetFilesToExtract(
        GameInstallation installation,
        EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var installDataFiles = GetInstallDataFiles(files);
        if (installDataFiles.Length != 3) throw new UnreachableException($"{nameof(SMAPIInstaller)} should guarantee with {nameof(Priority)} that {nameof(GetInstallDataFiles)} returns 3 files when called from {nameof(GetFilesToExtractAsync)} but it has {installDataFiles.Length} files instead!");

        KeyValuePair<RelativePath, AnalyzedFile> installDataFile;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            installDataFile = installDataFiles.First(kv => kv.Key.Parent.FileName.Equals("linux"));
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            installDataFile = installDataFiles.First(kv => kv.Key.Parent.FileName.Equals("macOS"));
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            installDataFile = installDataFiles.First(kv => kv.Key.Parent.FileName.Equals("windows"));
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        var (path, file) = installDataFile;
        if (file is not AnalyzedArchive archive)
            throw new UnreachableException($"{nameof(AnalyzedFile)} that has the file type {nameof(FileType.ZIP)} is not a {nameof(AnalyzedArchive)}");

        var archiveContents = archive.Contents;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // TODO: replace launcher script
            // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L386-L417
            var modLauncherScriptFile = archiveContents
                .First(kv => kv.Key.FileName.Equals("unix-launcher.sh"));

            // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L380-L384
            var basicFiles = archiveContents
                .Where(kv => !kv.Key.Equals("unix-launcher.sh"))
                .ToArray();
        }
        else
        {
            var basicFiles = archiveContents;
        }

        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L419-L425
        var gameDepsFilePath = installation.Locations
            .First(x => x.Key == GameFolderType.Game).Value
            .CombineUnchecked("Stardew Valley.deps.json");

        var gameDepsFile = installation.Game
            .GetGameFiles(installation, _dataStore)
            .FirstOrDefault(gameFile => gameFile.To.FileName.Equals("Stardew Valley.deps.json"));

        if (gameDepsFile is not null)
        {
            var newDepsFilePath = gameDepsFilePath.Parent.CombineUnchecked("StardewModdingAPI.deps.json");
            // TODO: copy the game file
        }

        throw new NotImplementedException();
        // return archive.Contents.Select(kv =>
        // {
        //     return new FromArchive
        //     {
        //         Id = ModFileId.New(),
        //         To = new GamePath(GameFolderType.Game, ),
        //         Size = kv.Value.Size,
        //         Hash = kv.Value.Hash,
        //         From = new HashRelativePath()
        //     };
        // })
    }
}
