using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for SMAPI itself. This is different from <see cref="SMAPIModInstaller"/>,
/// which is an implementation of <see cref="IModInstaller"/> for mods that use SMAPI.
/// </summary>
public class SMAPIInstaller : IModInstaller
{
    private static readonly RelativePath InstallDatFile = "install.dat".ToRelativePath();
    private static readonly RelativePath LinuxFolder = "linux".ToRelativePath();
    private static readonly RelativePath WindowsFolder = "windows".ToRelativePath();
    private static readonly RelativePath MacOSFolder = "macOS".ToRelativePath();

    private readonly IDataStore _dataStore;
    private readonly FileHashCache _fileHashCache;

    public SMAPIInstaller(IDataStore dataStore, FileHashCache fileHashCache)
    {
        _dataStore = dataStore;
        _fileHashCache = fileHashCache;
    }

    private static KeyValuePair<RelativePath, AnalyzedFile>[] GetInstallDataFiles(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var installDataFiles = files.Where(kv =>
        {
            var (path, file) = kv;
            var fileName = path.FileName;
            var parent = path.Parent.FileName;

            return file.FileTypes.Contains(FileType.ZIP) &&
                   fileName.Equals(InstallDatFile) &&
                   (parent.Equals(LinuxFolder) ||
                    parent.Equals(MacOSFolder) ||
                    parent.Equals(WindowsFolder));
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

    public ValueTask<IEnumerable<Mod>> GetModsAsync(
        GameInstallation gameInstallation,
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(gameInstallation, baseMod, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<Mod> GetMods(
        GameInstallation gameInstallation,
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var modFiles = new List<AModFile>();

        var installDataFiles = GetInstallDataFiles(archiveFiles);
        if (installDataFiles.Length != 3) throw new UnreachableException($"{nameof(SMAPIInstaller)} should guarantee with {nameof(Priority)} that {nameof(GetInstallDataFiles)} returns 3 files when called from {nameof(GetModsAsync)} but it has {installDataFiles.Length} files instead!");

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

        var gameFolderPath = gameInstallation.Locations
            .First(x => x.Key == GameFolderType.Game).Value;

        var archiveContents = archive.Contents;

        // TODO: install.dat is an archive inside an archive see https://github.com/Nexus-Mods/NexusMods.App/issues/244
        // the basicFiles have to be extracted from the nested archive and put inside the game folder
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L380-L384
        var basicFiles = archiveContents
            .Where(kv => !kv.Key.Equals("unix-launcher.sh")).ToArray();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // TODO: Replace game launcher (StardewValley) with unix-launcher.sh by overwriting the game file
            // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L386-L417
            var modLauncherScriptFile = archiveContents
                .First(kv => kv.Key.FileName.Equals("unix-launcher.sh"));

            var gameLauncherScriptFilePath = gameFolderPath.CombineUnchecked("StardewValley");
        }

        // TODO: for Xbox Game Pass: replace "Stardew Valley.exe" with "StardewModdingAPI.exe"

        // copy "Stardew Valley.deps.json" to "StardewModdingAPI.deps.json"
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L419-L425
        var gameDepsFilePath = gameFolderPath.CombineUnchecked("Stardew Valley.deps.json");
        if (!_fileHashCache.TryGetCached(gameDepsFilePath, out var gameDepsFileCache))
            throw new NotImplementedException($"Game file {gameFolderPath} was not found in cache!");

        modFiles.Add(new GameFile
        {
            Hash = gameDepsFileCache.Hash,
            Size = gameDepsFileCache.Size,
            Id = ModFileId.New(),
            Installation = gameInstallation,
            To = new GamePath(GameFolderType.Game, "StardewModdingAPI.deps.json")
        });

        yield return baseMod with
        {
            Files = modFiles.ToEntityDictionary(_dataStore)
        };
    }
}
