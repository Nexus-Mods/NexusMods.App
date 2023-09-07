using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for SMAPI itself. This is different from <see cref="SMAPIModInstaller"/>,
/// which is an implementation of <see cref="IModInstaller"/> for mods that use SMAPI.
/// </summary>
public class SMAPIInstaller : AModInstaller
{
    private static readonly RelativePath InstallDatFile = "install.dat".ToRelativePath();
    private static readonly RelativePath LinuxFolder = "linux".ToRelativePath();
    private static readonly RelativePath WindowsFolder = "windows".ToRelativePath();
    private static readonly RelativePath MacOSFolder = "macOS".ToRelativePath();

    private readonly IOSInformation _osInformation;
    private readonly FileHashCache _fileHashCache;
    private readonly IDownloadRegistry _downloadRegistry;

    private SMAPIInstaller(IOSInformation osInformation, FileHashCache fileHashCache, IDownloadRegistry downloadRegistry, IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _osInformation = osInformation;
        _fileHashCache = fileHashCache;
        _downloadRegistry = downloadRegistry;
    }

    private static FileTreeNode<RelativePath, ModSourceFileEntry>[] GetInstallDataFiles(FileTreeNode<RelativePath, ModSourceFileEntry> files)
    {
        var installDataFiles = files.GetAllDescendentFiles()
            .Where(kv =>
        {
            var (path, file) = kv;
            var fileName = path.FileName;
            var parent = path.Parent.FileName;

            return fileName.Equals(InstallDatFile) &&
                   (parent.Equals(LinuxFolder) ||
                    parent.Equals(MacOSFolder) ||
                    parent.Equals(WindowsFolder));
        })
            .ToArray();

        return installDataFiles;
    }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var modFiles = new List<AModFile>();

        var installDataFiles = GetInstallDataFiles(archiveFiles);
        if (installDataFiles.Length != 3)
            return NoResults;

        var installDataFile = _osInformation.MatchPlatform(
            state: ref installDataFiles,
            onWindows: (ref FileTreeNode<RelativePath, ModSourceFileEntry>[] dataFiles) => dataFiles.First(kv => kv.Path.Parent.FileName.Equals("windows")),
            onLinux: (ref FileTreeNode<RelativePath, ModSourceFileEntry>[] dataFiles) => dataFiles.First(kv => kv.Path.Parent.FileName.Equals("linux")),
            onOSX: (ref FileTreeNode<RelativePath, ModSourceFileEntry>[] dataFiles) => dataFiles.First(kv => kv.Path.Parent.FileName.Equals("macOS"))
        );

        var (path, file) = installDataFile;

        var found = _downloadRegistry.GetByHash(file!.Hash).ToArray();
        DownloadId downloadId;
        if (!found.Any())
            downloadId = await RegisterDataFile(path, file, cancellationToken);
        else
        {
            downloadId = found.First();
        }


        var gameFolderPath = gameInstallation.Locations
            .First(x => x.Key == GameFolderType.Game).Value;

        var archiveContents = (await _downloadRegistry.Get(downloadId)).GetFileTree();

        // TODO: install.dat is an archive inside an archive see https://github.com/Nexus-Mods/NexusMods.App/issues/244
        // the basicFiles have to be extracted from the nested archive and put inside the game folder
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L380-L384
        var basicFiles = archiveContents.GetAllDescendentFiles()
            .Where(kv => !kv.Path.Equals("unix-launcher.sh")).ToArray();

        if (_osInformation.IsLinux || _osInformation.IsOSX)
        {
            // TODO: Replace game launcher (StardewValley) with unix-launcher.sh by overwriting the game file
            // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L386-L417
            var modLauncherScriptFile = archiveContents.GetAllDescendentFiles()
                .First(kv => kv.Path.FileName.Equals("unix-launcher.sh"));

            var gameLauncherScriptFilePath = gameFolderPath.Combine("StardewValley");
        }

        // TODO: for Xbox Game Pass: replace "Stardew Valley.exe" with "StardewModdingAPI.exe"

        // copy "Stardew Valley.deps.json" to "StardewModdingAPI.deps.json"
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L419-L425
        var gameDepsFilePath = gameFolderPath.Combine("Stardew Valley.deps.json");
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

        // TODO: consider adding Name and Version
        return new [] { new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        }};
    }

    private async ValueTask<DownloadId> RegisterDataFile(RelativePath filename, ModSourceFileEntry file, CancellationToken token)
    {
        return await _downloadRegistry.RegisterDownload(file.StreamFactory, new FilePathMetadata { OriginalName = filename.FileName, Quality = Quality.Low}, token);
    }

    public static SMAPIInstaller Create(IServiceProvider serviceProvider)
    {
        return new SMAPIInstaller(
            serviceProvider.GetRequiredService<IOSInformation>(),
            serviceProvider.GetRequiredService<FileHashCache>(),
            serviceProvider.GetRequiredService<IDownloadRegistry>(),
            serviceProvider
        );
    }
}
