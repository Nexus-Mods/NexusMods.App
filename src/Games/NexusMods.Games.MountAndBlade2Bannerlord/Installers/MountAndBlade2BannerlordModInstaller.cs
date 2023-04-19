using System.Collections.Immutable;
using Bannerlord.LauncherManager.Models;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.MountAndBlade2Bannerlord.Analyzers;
using NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Installers;

public sealed class MountAndBlade2BannerlordModInstaller : IModInstaller
{
    public MountAndBlade2BannerlordModInstaller(LauncherManagerFactory launcherManagerFactory)
    {
        _launcherManagerFactory = launcherManagerFactory;
    }

    private readonly LauncherManagerFactory _launcherManagerFactory;

    // TODO: We had in mind creating optional mod types (Framework, Gameplay, Assets, etc) that we potentially could map to priorities
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (!installation.Is<MountAndBlade2Bannerlord>()) return Common.Priority.None;

        var launcherManager = _launcherManagerFactory.Get(installation);
        var result = launcherManager.TestModuleContent(files.Select(x => x.Key.ToString()).ToArray());

        return result.Supported
            ? Common.Priority.Normal
            : Common.Priority.None;
    }

    public ValueTask<IEnumerable<AModFile>> GetFilesToExtractAsync(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files, CancellationToken ct = default)
    {
        var filesToExtract = new List<AModFile>();

        var moduleInfos = files
            .Select(x => x.Value.AnalysisData.OfType<MountAndBlade2BannerlordModuleInfo>().FirstOrDefault() is { } data ? new ModuleInfoExtendedWithPath(data.ModuleInfo, x.Key.Path) : null)
            .OfType<ModuleInfoExtendedWithPath>()
            .ToArray();

        var launcherManager = _launcherManagerFactory.Get(installation);
        // InstallModuleContent will only install mods if the ModuleInfoExtendedWithPath for a mod was provided
        var result = launcherManager.InstallModuleContent(files.Select(x => x.Key.ToString()).ToArray(), moduleInfos);
        filesToExtract.AddRange(result.Instructions.OfType<CopyInstallInstruction>().Select(instruction =>
        {
            var relativePath = instruction.Source.ToRelativePath();
            var file = files[relativePath];
            return new FromArchive
            {
                Id = ModFileId.New(),
                To = new GamePath(GameFolderType.Game, MountAndBlade2BannerlordConstants.ModFolder.Join(relativePath)),
                From = new HashRelativePath(srcArchive, relativePath),
                Hash = file.Hash,
                Size = file.Size,
                Metadata = ImmutableHashSet.CreateRange<IModFileMetadata>(new List<IModFileMetadata>
                {
                    new ModuleIdMetadata { ModuleId = instruction.ModuleId },
                    new OriginalPathMetadata { OriginalRelativePath = relativePath.Path }
                })
            };
        }));

        if (filesToExtract.Count == 0) // Not a valid Bannerlord Module - install in root folder the content
        {
            filesToExtract.AddRange(files.Select(kv =>
            {
                var (path, file) = kv;
                return new FromArchive
                {
                    Id = ModFileId.New(),
                    To = new GamePath(GameFolderType.Game, path),
                    From = new HashRelativePath(srcArchive, path),
                    Hash = file.Hash,
                    Size = file.Size,
                };
            }));
        }

        return ValueTask.FromResult<IEnumerable<AModFile>>(filesToExtract);
    }
}
