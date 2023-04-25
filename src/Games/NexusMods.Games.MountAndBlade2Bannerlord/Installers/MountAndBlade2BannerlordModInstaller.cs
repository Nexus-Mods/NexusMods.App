using System.Collections.Immutable;
using System.Diagnostics;
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
    private readonly LauncherManagerFactory _launcherManagerFactory;
    
    public MountAndBlade2BannerlordModInstaller(LauncherManagerFactory launcherManagerFactory)
    {
        _launcherManagerFactory = launcherManagerFactory;
    }

    // TODO: We had in mind creating optional mod types (Framework, Gameplay, Assets, etc) that we potentially could map to priorities
    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (!installation.Is<MountAndBlade2Bannerlord>()) return Priority.None;

        var launcherManager = _launcherManagerFactory.Get(installation);
        var result = launcherManager.TestModuleContent(archiveFiles.Select(x => x.Key.ToString()).ToArray());

        return result.Supported ? Priority.Normal : Priority.None;
    }

    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation installation, ModId baseModId, Hash srcArchiveHash, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles, CancellationToken ct = default)
    {
        static IEnumerable<KeyValuePair<RelativePath, AnalyzedFile>> GetModuleInfoFiles(EntityDictionary<RelativePath, AnalyzedFile> files)
        {
            return files.Where(kv =>
            {
                var (path, file) = kv;

                if (!path.FileName.Equals(MountAndBlade2BannerlordConstants.SubModuleFile)) return false;
                return file.AnalysisData.OfType<MountAndBlade2BannerlordModuleInfo>().FirstOrDefault() is { } moduleInfo;
            });
        }

        var launcherManager = _launcherManagerFactory.Get(installation);
        
        var moduleInfoFiles = GetModuleInfoFiles(archiveFiles).ToArray();
        var mods = moduleInfoFiles.Select(moduleInfoFile =>
        {
            var parent = moduleInfoFile.Key.Parent;
            var moduleInfo = moduleInfoFile.Value.AnalysisData.OfType<MountAndBlade2BannerlordModuleInfo>().FirstOrDefault()?.ModuleInfo;
            
            if (moduleInfo is null) throw new UnreachableException();

            var moduleInfoWithPath = new ModuleInfoExtendedWithPath(moduleInfo, moduleInfoFile.Key.Path);
            // InstallModuleContent will only install mods if the ModuleInfoExtendedWithPath for a mod was provided
            var result = launcherManager.InstallModuleContent(archiveFiles.Where(kv => kv.Key.InFolder(parent)).Select(x => x.Key.ToString()).ToArray(), new[] { moduleInfoWithPath });
            var modFiles = result.Instructions.OfType<CopyInstallInstruction>().Select(instruction =>
            {
                var relativePath = instruction.Source.ToRelativePath();
                var file = archiveFiles[relativePath];
                return new FromArchive
                {
                    Id = ModFileId.New(),
                    To = new GamePath(GameFolderType.Game, MountAndBlade2BannerlordConstants.ModFolder.Join(relativePath)),
                    From = new HashRelativePath(srcArchiveHash, relativePath),
                    Hash = file.Hash,
                    Size = file.Size,
                    Metadata = ImmutableHashSet.CreateRange<IModFileMetadata>(new List<IModFileMetadata>
                    {
                        new ModuleIdMetadata { ModuleId = instruction.ModuleId },
                        new OriginalPathMetadata { OriginalRelativePath = relativePath.Path }
                    })
                };
            });
            return new ModInstallerResult
            {
                Id = baseModId,
                Files = modFiles,
                Name = moduleInfo.Name,
                Version = moduleInfo.Version.ToString()
            };
        });
        
        if (moduleInfoFiles.Length == 0) // Not a valid Bannerlord Module - install in root folder the content
        {
            var modFiles = archiveFiles.Select(kv =>
            {
                var (path, file) = kv;
                return new FromArchive
                {
                    Id = ModFileId.New(),
                    To = new GamePath(GameFolderType.Game, path),
                    From = new HashRelativePath(srcArchiveHash, path),
                    Hash = file.Hash,
                    Size = file.Size,
                };
            });
            mods = new List<ModInstallerResult>
            {
                new ModInstallerResult
                {
                    Id = baseModId,
                    Files = modFiles
                }
            };
        }

        return ValueTask.FromResult(mods);
    }
}
