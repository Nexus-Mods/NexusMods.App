using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathei.LinqGen;
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
using NexusMods.DataModel.Trees;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

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
    private readonly IFileOriginRegistry _fileOriginRegistry;

    private SMAPIInstaller(IOSInformation osInformation, FileHashCache fileHashCache, IFileOriginRegistry fileOriginRegistry, IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _osInformation = osInformation;
        _fileHashCache = fileHashCache;
        _fileOriginRegistry = fileOriginRegistry;
    }

    private static KeyedBox<RelativePath, ModFileTree>[] GetInstallDataFiles(KeyedBox<RelativePath, ModFileTree> files)
    {
        var installDataFiles = files.GetFiles()
            .Gen()
            .Where(kv =>
        {
            var fileName = kv.FileName();
            var parent = kv.Parent()!.Item.FileName;

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
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        var modFiles = new List<AModFile>();

        var installDataFiles = GetInstallDataFiles(archiveFiles);
        if (installDataFiles.Length != 3)
            return NoResults;

        var installDataFile = _osInformation.MatchPlatform(
            state: ref installDataFiles,
            onWindows: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("windows")),
            onLinux: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("linux")),
            onOSX: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("macOS"))
        );

        var found = _fileOriginRegistry.GetByHash(installDataFile.Item!.Hash).ToArray();
        DownloadId downloadId;
        if (!found.Any())
            downloadId = await RegisterDataFile(installDataFile, cancellationToken);
        else
        {
            downloadId = found.First();
        }

        var gameFolderPath = gameInstallation.LocationsRegister[LocationId.Game];
        var archiveContents = (await _fileOriginRegistry.Get(downloadId)).GetFileTree();

        // TODO: install.dat is an archive inside an archive see https://github.com/Nexus-Mods/NexusMods.App/issues/244
        // the basicFiles have to be extracted from the nested archive and put inside the game folder
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L380-L384
        var basicFiles = archiveContents.EnumerateChildrenBfs()
            .Where(kv => !kv.Value.Item.Path.Equals("unix-launcher.sh")).ToArray();

        if (_osInformation.IsLinux || _osInformation.IsOSX)
        {
            // TODO: Replace game launcher (StardewValley) with unix-launcher.sh by overwriting the game file
            // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L386-L417
            var modLauncherScriptFile = archiveContents.EnumerateChildrenBfs()
                .First(kv => kv.Value.Item.Segment.Equals("unix-launcher.sh"));

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
            Id = ModFileId.NewId(),
            Installation = gameInstallation,
            To = new GamePath(LocationId.Game, "StardewModdingAPI.deps.json")
        });

        // TODO: consider adding Name and Version
        return new [] { new ModInstallerResult
        {
            Id = baseModId,
            Files = modFiles
        }};
    }

    private async ValueTask<DownloadId> RegisterDataFile(KeyedBox<RelativePath, ModFileTree> node, CancellationToken token)
    {
        return await _fileOriginRegistry.RegisterDownload(node.Item.StreamFactory!, new FilePathMetadata { OriginalName = node.Item.FileName, Quality = Quality.Low }, token);
    }

    public static SMAPIInstaller Create(IServiceProvider serviceProvider)
    {
        return new SMAPIInstaller(
            serviceProvider.GetRequiredService<IOSInformation>(),
            serviceProvider.GetRequiredService<FileHashCache>(),
            serviceProvider.GetRequiredService<IFileOriginRegistry>(),
            serviceProvider
        );
    }
}
