using System.Xml;
using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;
using NexusMods.Games.MountAndBlade2Bannerlord.Sorters;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
using static NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Installers;

public sealed class MountAndBlade2BannerlordModInstaller : AModInstaller
{
    private readonly LauncherManagerFactory _launcherManagerFactory;

    private MountAndBlade2BannerlordModInstaller(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _launcherManagerFactory = serviceProvider.GetRequiredService<LauncherManagerFactory>();
    }

    public static MountAndBlade2BannerlordModInstaller Create(IServiceProvider serviceProvider) => new(serviceProvider);

    private static IAsyncEnumerable<(FileTreeNode<RelativePath, ModSourceFileEntry> ModuleInfoFile, ModuleInfoExtended ModuleInfo)> GetModuleInfoFiles(
        FileTreeNode<RelativePath, ModSourceFileEntry> files)
    {
        return files.GetAllDescendentFiles().SelectAsync(async kv =>
        {
            var (path, file) = kv;

            if (!path.FileName.Equals(SubModuleFile))
                return default;

            await using var stream = await file!.Open();
            try
            {
                var doc = new XmlDocument();
                doc.Load(stream);
                var data = ModuleInfoExtended.FromXml(doc);
                return (ModuleInfoFile: kv, ModuleInfo: data);
            }
            catch (Exception e)
            {
                return default;
                //_logger.LogError("Failed to Parse Bannerlord Module: {EMessage}\\n{EStackTrace}", e.Message, e.StackTrace);
            }
        }).Where(kv => kv.ModuleInfo != null!);
    }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation installation, LoadoutId loadoutId, ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken ct = default)
    {
        var moduleInfoFiles = await GetModuleInfoFiles(archiveFiles).ToArrayAsync(ct);

        // Not a valid Bannerlord Module - install in root folder the content
        if (!moduleInfoFiles.Any())
        {
            //return NoResults;

            var modFiles = archiveFiles.GetAllDescendentFiles().Select(kv =>
            {
                var (path, file) = kv;
                var moduleRoot = path.Parent;

                return file!.ToStoredFile(new GamePath(LocationId.Game, ModFolder.Join(path.DropFirst(moduleRoot.Depth - 1))));
            });
            return new List<ModInstallerResult>
            {
                new ModInstallerResult
                {
                    Id = baseModId,
                    Files = modFiles
                }
            };
        }

        var launcherManager = _launcherManagerFactory.Get(installation);

        return moduleInfoFiles.Select(node =>
        {
            var (moduleInfoFile, moduleInfo) = node;
            var moduleRoot = moduleInfoFile.Parent;
            var moduleInfoWithPath = new ModuleInfoExtendedWithPath(moduleInfo, moduleInfoFile.Path);

            // InstallModuleContent will only install mods if the ModuleInfoExtendedWithPath for a mod was provided
            var result = launcherManager.InstallModuleContent(moduleRoot.GetAllDescendentFiles().Select(x => x.Path.ToString()).ToArray(), new[] { moduleInfoWithPath });
            var modFiles = result.Instructions.OfType<CopyInstallInstruction>().Select(instruction =>
            {
                static IEnumerable<IMetadata> GetMetadata(ModuleInfoExtendedWithPath moduleInfo, RelativePath relativePath)
                {
                    yield return new ModuleFileMetadata { OriginalRelativePath = relativePath.Path };
                    if (relativePath.Equals(SubModuleFile)) yield return new SubModuleFileMetadata
                    {
                        IsValid = true, // TODO: For now lets keep it true while we don't have the validation layer
                        ModuleInfo = moduleInfo
                    };
                }

                var relativePath = instruction.Source.ToRelativePath();
                var (path, file) = moduleRoot.FindNode(relativePath)!;

                var fromArchive = file!.ToStoredFile(new GamePath(LocationId.Game, ModFolder.Join(path.DropFirst(moduleRoot.Depth - 1))));
                return fromArchive with
                {
                    Metadata = fromArchive.Metadata.AddRange(GetMetadata(moduleInfoWithPath, relativePath))
                };
            });

            return new ModInstallerResult
            {
                Id = ModId.NewId(),
                Files = modFiles,
                Name = moduleInfo.Name,
                Version = moduleInfo.Version.ToString(),
                SortRules = new []
                {
                    new ModuleInfoSort()
                },
            };
        });
    }
}
