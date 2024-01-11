using System.Diagnostics;
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
    private sealed record ModuleInfoFile(FileTreeNode<RelativePath, ModSourceFileEntry> File, ModuleInfoExtended ModuleInfo);


    private MountAndBlade2BannerlordModInstaller(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static MountAndBlade2BannerlordModInstaller Create(IServiceProvider serviceProvider) => new(serviceProvider);

    private static IAsyncEnumerable<ModuleInfoFile> GetModuleInfoFiles(FileTreeNode<RelativePath, ModSourceFileEntry> files) => files.GetAllDescendentFiles().SelectAsync(async n =>
    {
        if (n is not { Path: var path, Value: { } file })
            return null;

        if (!path.FileName.Equals(SubModuleFile))
            return null;

        await using var stream = await file.Open();
        try
        {
            var doc = new XmlDocument();
            doc.Load(stream);
            var data = ModuleInfoExtended.FromXml(doc);
            return new ModuleInfoFile(n, data);
        }
        catch (Exception)
        {
            return null;
            //_logger.LogError("Failed to Parse Bannerlord Module: {EMessage}\\n{EStackTrace}", e.Message, e.StackTrace);
        }
    }).OfType<ModuleInfoFile>();

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation installation, LoadoutId loadoutId, ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken ct = default)
    {
        var moduleInfoFiles = await GetModuleInfoFiles(archiveFiles).ToArrayAsync(ct);

        // Not a valid Bannerlord Module - install in root folder the content
        if (moduleInfoFiles.Length == 0)
        {
            //return NoResults;

            var modFiles = archiveFiles.GetAllDescendentFiles().Select(kv =>
            {
                if (kv is not { Path: var path, Value: { } file })
                    throw new UnreachableException();

                var moduleRoot = path.Parent;
                return file.ToStoredFile(new GamePath(LocationId.Game, ModFolder.Join(path.DropFirst(moduleRoot.Depth - 1))));
            });
            return new List<ModInstallerResult>
            {
                new()
                {
                    Id = baseModId,
                    Files = modFiles
                }
            };
        }

        var launcherManager = installation.ServiceScope.ServiceProvider.GetRequiredService<LauncherManagerNexusMods>();

        return moduleInfoFiles.Select(node =>
        {
            var (moduleInfoFile, moduleInfo) = node;
            var moduleRoot = moduleInfoFile.Parent;
            var moduleInfoWithPath = new ModuleInfoExtendedWithPath(moduleInfo, moduleInfoFile.Path);

            // InstallModuleContent will only install mods if the ModuleInfoExtendedWithPath for a mod was provided
            var result = launcherManager.InstallModuleContent(moduleRoot.GetAllDescendentFiles().Select(x => x.Path.ToString()).ToArray(), new[] { moduleInfoWithPath });
            var modFiles = result.Instructions.OfType<CopyInstallInstruction>().Select(instruction =>
            {
                static IEnumerable<IMetadata> GetMetadata(ModuleInfoExtended moduleInfo, RelativePath relativePath)
                {
                    yield return new ModuleFileMetadata { OriginalRelativePath = relativePath.Path };
                    if (relativePath.Equals(SubModuleFile)) yield return new SubModuleFileMetadata
                    {
                        IsValid = true, // TODO: For now lets keep it true while we don't have the validation layer
                        ModuleInfo = moduleInfo
                    };
                }

                var relativePath = instruction.Source.ToRelativePath();
                if (moduleRoot.FindNode(relativePath) is not { Path: var path, Value: { } file })
                    throw new UnreachableException();

                var fromArchive = file.ToStoredFile(new GamePath(LocationId.Game, ModFolder.Join(path.DropFirst(moduleRoot.Depth - 1))));
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
