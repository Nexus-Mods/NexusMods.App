using System.Xml;
using Bannerlord.ModuleManager;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using static NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Installers;

public sealed class MountAndBlade2BannerlordModInstaller : AModInstaller
{
    private MountAndBlade2BannerlordModInstaller(IServiceProvider serviceProvider) : base(serviceProvider) { }

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
        }).Where(kv => kv.ModuleInfo != null);
    }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(GameInstallation gameInstallation, ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, CancellationToken ct = default)
    {
        var moduleInfoFiles = await GetModuleInfoFiles(archiveFiles).ToArrayAsync(ct);

        if (!moduleInfoFiles.Any()) return NoResults;

        var mods = moduleInfoFiles.Select(found =>
        {
            var (moduleInfoFile, moduleInfo) = found;
            var parent = moduleInfoFile.Parent;

            var modFiles = parent.GetAllDescendentFiles().Select(kv =>
            {
                var (path, file) = kv;
                return file!.ToFromArchive(new GamePath(LocationId.Game, ModFolder.Join(path.DropFirst(parent.Depth - 1))));
            });

            return new ModInstallerResult
            {
                Id = ModId.New(),
                Files = modFiles,
                Name = moduleInfo.Name,
                Version = moduleInfo.Version.ToString()
            };
        });

        return mods;
    }
}
