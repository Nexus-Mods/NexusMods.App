using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Models;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.MountAndBladeBannerlord;

public class MountAndBladeBannerlordModInstaller : IModInstaller
{
    public MountAndBladeBannerlordModInstaller(NexusModsBannerlordLauncherManagerFactory launcherManagerFactory, IDataStore store)
    {
        _launcherManagerFactory = launcherManagerFactory;
        _store = store;
    }
    
    private readonly RelativePath ModFolder = Constants.ModulesFolder.ToRelativePath();
    private readonly RelativePath SubModuleFile = Constants.SubModuleName.ToRelativePath();
    private readonly NexusModsBannerlordLauncherManagerFactory _launcherManagerFactory;
    private readonly IDataStore _store;

    // TODO: We had in mind creating optional mod types (Framework, Gameplay, Assets, etc) that we potentially could map to priorities
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (installation.Game is not MountAndBladeBannerlord) return Common.Priority.None;

        var launcherManager = _launcherManagerFactory.Get(installation);
        var result = launcherManager.TestModuleContent(files.Select(x => x.Key.ToString()).ToArray());
        
        return result.Supported 
            ? Common.Priority.Normal 
            : Common.Priority.None;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var modFolder = files.Keys.First(m => m.FileName == SubModuleFile).Parent;
        
        var launcherManager = _launcherManagerFactory.Get(installation);
        var result = launcherManager.InstallModuleContent(files.Select(x => x.Key.ToString()).ToArray(), modFolder.ToString());
        return result.Instructions.Where(x => x.Type == InstallInstructionType.Copy).Cast<CopyInstallInstruction>().Select(instruction =>
        {
            var relativePath = instruction.Source.ToRelativePath();
            var file = files[relativePath];
            return new FromArchive
            {
                Id = ModFileId.New(),
                To = new GamePath(GameFolderType.Game, ModFolder.Join(relativePath)),
                From = new HashRelativePath(srcArchive, relativePath),
                Hash = file.Hash,
                Size = file.Size,
                Store = _store
            };
        });
    }
}