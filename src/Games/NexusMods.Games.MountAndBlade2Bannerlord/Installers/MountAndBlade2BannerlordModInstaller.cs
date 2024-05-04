using System.Xml;
using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Extensions.BCL;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using static NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Installers;

public sealed class MountAndBlade2BannerlordModInstaller : AModInstaller
{
    private readonly LauncherManagerFactory _launcherManagerFactory;
    private readonly IConnection _conn;

    private MountAndBlade2BannerlordModInstaller(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _launcherManagerFactory = serviceProvider.GetRequiredService<LauncherManagerFactory>();
        _conn = serviceProvider.GetRequiredService<IConnection>();
    }

    public static MountAndBlade2BannerlordModInstaller Create(IServiceProvider serviceProvider) => new(serviceProvider);

    private static IAsyncEnumerable<(KeyedBox<RelativePath, ModFileTree> ModuleInfoFile, ModuleInfoExtended ModuleInfo)> GetModuleInfoFiles(
        KeyedBox<RelativePath, ModFileTree> files)
    {
        return files.GetFiles().SelectAsync(async kv =>
        {
            var path = kv.Path();
            if (!path.FileName.Equals(SubModuleFile))
                return default;

            await using var stream = await kv.Item.OpenAsync();
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

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(ModInstallerInfo info, CancellationToken ct = default)
    {
        var moduleInfoFiles = await GetModuleInfoFiles(info.ArchiveFiles).ToArrayAsync(ct);

        // Not a valid Bannerlord Module - install in root folder the content
        if (!moduleInfoFiles.Any())
        {
            //return NoResults;

            var modFiles = info.ArchiveFiles.GetFiles().Select(kv => kv.ToStoredFile(new GamePath(LocationId.Game, kv.Path()))).AsEnumerable();
            return new List<ModInstallerResult>
            {
                new()
                {
                    Id = info.BaseModId,
                    Files = modFiles
                }
            };
        }

        var launcherManager = _launcherManagerFactory.Get(info);
        
        var results = new List<ModInstallerResult>();

        foreach (var node in moduleInfoFiles)
        {
            var (moduleInfoFile, moduleInfo) = node;
            var moduleRoot = moduleInfoFile.Parent();
            var moduleInfoWithPath = new ModuleInfoExtendedWithPath(moduleInfo, moduleInfoFile.Path());
            
            using var tx = _conn.BeginTransaction();
            var moduleInfoId = MnemonicDB.ModuleInfoExtended.AddTo(moduleInfoWithPath, tx);
            var txResult = await tx.Commit();
            var moduleInfoIdEntity = txResult[moduleInfoId];
            
            
            // InstallModuleContent will only install mods if the ModuleInfoExtendedWithPath for a mod was provided
            var result = launcherManager.InstallModuleContent(moduleRoot!.GetFiles().Select(x => x.Path().ToString()).ToArray(), new[] { moduleInfoWithPath });
            var modFiles = result.Instructions.OfType<CopyInstallInstruction>().Select(instruction =>
            {
                static TempEntity WithMetaData(TempEntity src, EntityId moduleInfo, RelativePath relativePath)
                {
                    src.Add(MnemonicDB.ModuleFileMetadata.OriginalRelativePath, relativePath.Path);

                    if (relativePath.Equals(SubModuleFile))
                    {
                        // TODO: For now lets keep it true while we don't have the validation layer
                        src.Add(MnemonicDB.SubModuleFileMetadata.IsValid, true); 
                        src.Add(MnemonicDB.SubModuleFileMetadata.ModuleInfo, moduleInfo);
                    }

                    return src;
                }

                var relativePath = instruction.Source.ToRelativePath();
                var node = info.ArchiveFiles.FindByPathFromChild(relativePath)!;
                var path = node.Path();
                var parent = moduleRoot!.Parent();
                var modulesFolderDepth = parent?.Depth() ?? 0;

                var fromArchive = node.ToStoredFile(new GamePath(LocationId.Game, ModFolder.Join(path.DropFirst(modulesFolderDepth))));

                return WithMetaData(fromArchive, moduleInfoIdEntity, relativePath);

            });

            var installerResult = new ModInstallerResult
            {
                Id = info.BaseModId,
                Files = modFiles,
                Name = moduleInfo.Name,
                Version = moduleInfo.Version.ToString()
            };
            results.Add(installerResult);
        }

        return results;

    }
}
